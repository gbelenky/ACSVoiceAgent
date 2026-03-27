using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Text.Json;
using ACSVoiceAgent;
using ACSVoiceAgent.Models;
using ACSVoiceAgent.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
builder.Services.AddApplicationInsightsTelemetry();

var acsConnectionString = builder.Configuration.GetValue<string>("AcsConnectionString");
ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

var client = new CallAutomationClient(acsConnectionString);

builder.Services.AddSingleton(client);
builder.Services.AddSingleton<CallSessionManager>();

var app = builder.Build();

var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');
if (string.IsNullOrEmpty(appBaseUrl))
{
    appBaseUrl = builder.Configuration.GetValue<string>("DevTunnelUri")?.TrimEnd('/');
}
if (string.IsNullOrEmpty(appBaseUrl))
{
    appBaseUrl = $"https://{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}";
}

app.UseWebSockets();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("App base URL resolved to: {BaseUrl}", appBaseUrl);

app.MapGet("/", () => "ACS Voice Agent with Voice Live SDK");

// --- Incoming Call (EventGrid) ---
app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    ILogger<Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        logger.LogInformation("Incoming Call event received");

        // Handle EventGrid subscription validation
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            if (eventData is SubscriptionValidationEventData subscriptionValidation)
            {
                return Results.Ok(new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidation.ValidationCode
                });
            }
        }

        var jsonObject = Helper.GetJsonObject(eventGridEvent.Data);
        var callerId = Helper.GetCallerId(jsonObject);
        var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
        logger.LogInformation("Incoming call from {CallerId}", callerId);
        var callbackUri = new Uri(new Uri(appBaseUrl!), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        var websocketUri = appBaseUrl!.Replace("https", "wss") + "/ws";

        logger.LogInformation("Callback URL: {CallbackUri}", callbackUri);
        logger.LogInformation("WebSocket URL: {WebSocketUri}", websocketUri);

        var mediaStreamingOptions = new MediaStreamingOptions(MediaStreamingAudioChannel.Mixed)
        {
            TransportUri = new Uri(websocketUri),
            MediaStreamingContent = MediaStreamingContent.Audio,
            StartMediaStreaming = true,
            EnableBidirectional = true,
            AudioFormat = AudioFormat.Pcm24KMono
        };

        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        var answerCallResult = await client.AnswerCallAsync(options);
        logger.LogInformation("Answered call. Connection ID: {ConnectionId}",
            answerCallResult.Value.CallConnection.CallConnectionId);
    }
    return Results.Ok();
});

// --- ACS Callback Events ---
app.MapPost("/api/callbacks/{contextId}", (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    CallSessionManager sessionManager,
    ILogger<Program> logger) =>
{
    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation("Call event received: {EventType}", @event.GetType().Name);

        if (@event is CallConnected)
        {
            logger.LogInformation("Call connected. ConnectionId: {ConnectionId}, CorrelationId: {CorrelationId}",
                @event.CallConnectionId, @event.CorrelationId);

            sessionManager.CreateSession(@event.CallConnectionId, @event.CorrelationId, callerId);
        }
        else if (@event is CallDisconnected)
        {
            logger.LogInformation("Call disconnected. CorrelationId: {CorrelationId}", @event.CorrelationId);
            sessionManager.RemoveSession(@event.CorrelationId);
        }
        else if (@event is CallTransferAccepted)
        {
            logger.LogInformation("Call transfer accepted");
        }
        else if (@event is CallTransferFailed transferFailed)
        {
            logger.LogError("Call transfer failed: {Details}",
                transferFailed.ResultInformation?.Message);
        }
    }
});

// --- WebSocket Endpoint for ACS Media Streaming ---
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
        await HandleWebSocket(context);
    else
        await next(context);
});

app.Run();

async Task HandleWebSocket(HttpContext context)
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var sessionManager = context.RequestServices.GetRequiredService<CallSessionManager>();
    var config = context.RequestServices.GetRequiredService<IConfiguration>();

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    logger.LogInformation("ACS WebSocket connected");

    var mediaHandler = new AcsMediaStreamingHandler(webSocket,
        context.RequestServices.GetRequiredService<ILogger<AcsMediaStreamingHandler>>());

    var voiceLiveService = new AzureVoiceLiveService(
        mediaHandler, client, config,
        context.RequestServices.GetRequiredService<ILogger<AzureVoiceLiveService>>());

    // Wait for CallConnected to create the session (Channel-based, no polling)
    var session = await sessionManager.WaitForSessionAsync(TimeSpan.FromSeconds(30));

    if (session != null)
    {
        session.AcsWebSocket = webSocket;
        logger.LogInformation("Session bound: CallConnectionId={CallConnectionId}, CallerId={CallerId}",
            session.CallConnectionId, session.CallerId);
    }
    else
    {
        logger.LogWarning("No call session found after 30s, starting Voice Live without session tracking");
        session = new CallSession();
    }

    await voiceLiveService.StartAsync(session);

    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var (message, endOfStream) = await mediaHandler.ReceiveMessageAsync(CancellationToken.None);
            if (endOfStream || message == null) break;

            try
            {
                var jsonDoc = JsonDocument.Parse(message);
                if (jsonDoc.RootElement.GetProperty("kind").GetString() == "AudioData")
                {
                    var audioData = jsonDoc.RootElement
                        .GetProperty("audioData")
                        .GetProperty("data")
                        .GetString();

                    if (audioData != null)
                        await voiceLiveService.SendAudioAsync(Convert.FromBase64String(audioData));
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse ACS message");
            }
        }
    }
    catch (WebSocketException ex)
    {
        logger.LogInformation("ACS WebSocket closed: {Message}", ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error in WebSocket audio loop");
    }
    finally
    {
        voiceLiveService.Close();
        logger.LogInformation("Media streaming session ended");
    }
}
