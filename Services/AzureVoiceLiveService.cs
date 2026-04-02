using System.Text.Json;
using Azure;
using Azure.AI.VoiceLive;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using ACSVoiceAgent.Models;

namespace ACSVoiceAgent.Services;

public class AzureVoiceLiveService
{
    private readonly AcsMediaStreamingHandler _mediaStreaming;
    private readonly CallAutomationClient _callClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureVoiceLiveService> _logger;
    private VoiceLiveSession? _session;
    private CallSession? _callSession;
    private CancellationTokenSource? _cts;
    private bool _greetingSent;

    public AzureVoiceLiveService(
        AcsMediaStreamingHandler mediaStreaming,
        CallAutomationClient callClient,
        IConfiguration configuration,
        ILogger<AzureVoiceLiveService> logger)
    {
        _mediaStreaming = mediaStreaming;
        _callClient = callClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CallSession callSession)
    {
        _callSession = callSession;
        _cts = callSession.Cts ?? new CancellationTokenSource();

        await CreateSessionAsync();
    }

    private async Task CreateSessionAsync()
    {
        var apiKey = _configuration.GetValue<string>("AzureVoiceLiveApiKey");
        ArgumentNullException.ThrowIfNullOrEmpty(apiKey);

        var endpoint = _configuration.GetValue<string>("AzureVoiceLiveEndpoint");
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

        var model = _configuration.GetValue<string>("VoiceLiveModel");
        ArgumentNullException.ThrowIfNullOrEmpty(model);

        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "system-prompt.txt");
        _logger.LogInformation("Loading system prompt from: {PromptPath}", promptPath);
        var systemPrompt = File.ReadAllText(promptPath);
        _logger.LogInformation("System prompt loaded ({Length} chars)", systemPrompt.Length);

        var client = new VoiceLiveClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        var sessionOptions = new VoiceLiveSessionOptions
        {
            Model = model,
            Instructions = systemPrompt,
            Voice = new AzureStandardVoice("en-US-Ava:DragonHDLatestNeural"),
            InputAudioFormat = InputAudioFormat.Pcm16,
            OutputAudioFormat = OutputAudioFormat.Pcm16,
            InputAudioNoiseReduction = new AudioNoiseReduction(AudioNoiseReductionType.FarField),
            InputAudioEchoCancellation = new AudioEchoCancellation(),
            TurnDetection = new ServerVadTurnDetection
            {
                Threshold = 0.5f,
                PrefixPadding = TimeSpan.FromMilliseconds(300),
                SilenceDuration = TimeSpan.FromMilliseconds(500)
            }
        };

        sessionOptions.Modalities.Clear();
        sessionOptions.Modalities.Add(InteractionModality.Text);
        sessionOptions.Modalities.Add(InteractionModality.Audio);

        // Tool definitions (per official SDK pattern)
        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("customer_lookup")
        {
            Description = "Look up a customer's account information by phone number or customer ID.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "identifier": { "type": "string", "description": "Customer phone number or customer ID" }
                    },
                    "required": ["identifier"]
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("order_status")
        {
            Description = "Check the status of an order by order ID.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "order_id": { "type": "string", "description": "The order ID to look up" }
                    },
                    "required": ["order_id"]
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("check_appointment")
        {
            Description = "Check a customer's upcoming appointments.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "customer_id": { "type": "string", "description": "Customer ID" }
                    },
                    "required": ["customer_id"]
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("book_appointment")
        {
            Description = "Book a new appointment for a customer.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "customer_id": { "type": "string", "description": "Customer ID" },
                        "date": { "type": "string", "description": "Appointment date in YYYY-MM-DD format" },
                        "time": { "type": "string", "description": "Appointment time in HH:MM format" }
                    },
                    "required": ["customer_id", "date", "time"]
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("cancel_appointment")
        {
            Description = "Cancel an existing appointment by appointment ID.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "appointment_id": { "type": "string", "description": "The appointment ID to cancel" }
                    },
                    "required": ["appointment_id"]
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("search_knowledge_base")
        {
            Description = "Search the company knowledge base for answers to frequently asked questions.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "query": { "type": "string", "description": "The search query or question" }
                    },
                    "required": ["query"]
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("transfer_call")
        {
            Description = "Transfer the current call to a human agent. Use when the customer requests to speak to a human or when you cannot resolve their issue.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "reason": { "type": "string", "description": "Brief reason for the transfer" }
                    }
                }
                """)
        });

        sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("end_call")
        {
            Description = "End the current phone call. You MUST call this tool IMMEDIATELY when the caller says goodbye or indicates they are done. Do not speak before calling this tool — call it first, then say goodbye after it returns.",
            Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "reason": { "type": "string", "description": "Brief reason for ending the call" }
                    }
                }
                """)
        });

        _logger.LogInformation("Connecting to Voice Live at {Endpoint} with {ToolCount} tools...",
            endpoint, sessionOptions.Tools.Count);

        _session = await client.StartSessionAsync(sessionOptions, _cts!.Token);

        if (_callSession != null)
        {
            _callSession.VoiceLiveSession = _session;
        }

        _logger.LogInformation("Connected to Voice Live successfully");

        _ = Task.Run(() => ReceiveEventsAsync(_cts.Token));
    }

    private async Task ReceiveEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var serverEvent in _session!.GetUpdatesAsync(cancellationToken))
            {
                await HandleSessionUpdateAsync(serverEvent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Voice Live receive loop cancelled");
        }
        catch (ObjectDisposedException)
        {
            _logger.LogInformation("Voice Live session disposed (call ended or transferred)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Voice Live receive loop");
        }
    }

    private async Task HandleSessionUpdateAsync(SessionUpdate serverEvent, CancellationToken cancellationToken)
    {
        switch (serverEvent)
        {
            case SessionUpdateSessionCreated:
                _logger.LogInformation("Voice Live session created");
                break;

            case SessionUpdateSessionUpdated:
                _logger.LogInformation("Voice Live session updated and ready");
                if (!_greetingSent)
                {
                    _greetingSent = true;
                    await SendProactiveGreetingAsync(cancellationToken);
                }
                break;

            case SessionUpdateResponseAudioDelta audioDelta:
                var outbound = OutStreamingData.GetAudioDataForOutbound(audioDelta.Delta.ToArray());
                await _mediaStreaming.SendMessageAsync(outbound);
                break;

            case SessionUpdateInputAudioBufferSpeechStarted:
                _logger.LogInformation("Speech started (barge-in)");
                await _mediaStreaming.SendMessageAsync(OutStreamingData.GetStopAudioForOutbound());
                break;

            case SessionUpdateResponseFunctionCallArgumentsDone functionCall:
                await HandleFunctionCallAsync(functionCall.Name, functionCall.CallId,
                    functionCall.Arguments, cancellationToken);
                break;

            case SessionUpdateResponseAudioTranscriptDone transcriptDone:
                _logger.LogInformation("Agent transcript: {Transcript}", transcriptDone.Transcript);
                _callSession?.Transcript.Add(new TranscriptEntry
                {
                    Speaker = "Agent",
                    Text = transcriptDone.Transcript
                });
                break;

            case SessionUpdateResponseAudioDone:
                _logger.LogDebug("Audio response completed");
                break;

            case SessionUpdateResponseDone:
                _logger.LogDebug("Response completed");
                break;

            case SessionUpdateError errorEvent:
                _logger.LogWarning("Voice Live error: {Code} - {Message}",
                    errorEvent.Error?.Code, errorEvent.Error?.Message);
                break;

            default:
                _logger.LogDebug("Voice Live event: {EventType}", serverEvent.GetType().Name);
                break;
        }
    }

    private async Task SendProactiveGreetingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending proactive greeting request");
        try
        {
            await _session!.StartResponseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send proactive greeting");
        }
    }

    private async Task HandleFunctionCallAsync(string functionName, string callId, string arguments,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Function call: {Function} with call_id: {CallId}, args: {Arguments}", functionName, callId, arguments);

        string result;
        try
        {
            var args = JsonDocument.Parse(arguments).RootElement;

            result = functionName switch
            {
                "customer_lookup" => AgentFunctions.LookupCustomer(args),
                "order_status" => AgentFunctions.CheckOrderStatus(args),
                "check_appointment" => AgentFunctions.CheckAppointment(args),
                "book_appointment" => AgentFunctions.BookAppointment(args),
                "cancel_appointment" => AgentFunctions.CancelAppointment(args),
                "search_knowledge_base" => AgentFunctions.SearchKnowledgeBase(args),
                "transfer_call" => await TransferCallAsync(args),
                "end_call" => await EndCallAsync(args),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };

            _callSession?.ToolInvocations.Add(new ToolInvocation
            {
                ToolName = functionName,
                Arguments = arguments,
                Result = result
            });

            _logger.LogInformation("Function {Function} completed with result: {Result}", functionName, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {Function}", functionName);
            result = JsonSerializer.Serialize(new { error = $"Function execution failed: {ex.Message}" });
        }

        // For end_call: schedule hangup with enough delay for the goodbye audio to play out
        if (functionName == "end_call")
        {
            _ = HangUpAfterDelayAsync();
        }

        await _session!.AddItemAsync(new FunctionCallOutputItem(callId, result), cancellationToken);
        await _session!.StartResponseAsync(cancellationToken);
    }

    private async Task<string> TransferCallAsync(JsonElement args)
    {
        var targetPhone = _configuration.GetValue<string>("TransferPhoneNumber");
        ArgumentNullException.ThrowIfNullOrEmpty(targetPhone, "TransferPhoneNumber");

        var reason = args.TryGetProperty("reason", out var r) ? r.GetString() ?? "Customer request" : "Customer request";

        var callConnectionId = _callSession?.CallConnectionId;
        if (string.IsNullOrEmpty(callConnectionId))
            return JsonSerializer.Serialize(new { error = "No active call connection for transfer" });

        _logger.LogInformation("Transferring call {CallConnectionId} to {Target}. Reason: {Reason}",
            callConnectionId, targetPhone, reason);

        var callConnection = _callClient.GetCallConnection(callConnectionId);
        await callConnection.TransferCallToParticipantAsync(new PhoneNumberIdentifier(targetPhone));

        return JsonSerializer.Serialize(new { status = "Transfer initiated", target = targetPhone, reason });
    }

    private Task<string> EndCallAsync(JsonElement args)
    {
        var reason = args.TryGetProperty("reason", out var r) ? r.GetString() ?? "Call completed" : "Call completed";

        var callConnectionId = _callSession?.CallConnectionId;
        if (string.IsNullOrEmpty(callConnectionId))
        {
            _logger.LogWarning("EndCallAsync: No active call connection ID — session may not be bound");
            return Task.FromResult(JsonSerializer.Serialize(new { error = "No active call connection" }));
        }

        _logger.LogWarning("End call requested for {CallConnectionId}. Reason: {Reason}", callConnectionId, reason);

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            status = "Call will end in a few seconds",
            reason,
            instruction = "Say a single brief goodbye sentence to the caller. Keep it very short."
        }));
    }

    private async Task HangUpAfterDelayAsync()
    {
        var callConnectionId = _callSession?.CallConnectionId;
        if (string.IsNullOrEmpty(callConnectionId))
        {
            _logger.LogWarning("HangUpAfterDelayAsync: No call connection ID available");
            return;
        }

        _logger.LogWarning("Waiting 6s before hanging up call {CallConnectionId}...", callConnectionId);
        // Allow enough time for the goodbye audio to be generated and played to the caller
        await Task.Delay(6000);

        _logger.LogWarning("Hanging up call {CallConnectionId}", callConnectionId);
        try
        {
            var callConnection = _callClient.GetCallConnection(callConnectionId);
            await callConnection.HangUpAsync(true);
            _logger.LogWarning("HangUpAsync completed successfully for {CallConnectionId}", callConnectionId);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Call {CallConnectionId} already disconnected (404)", callConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hanging up call {CallConnectionId}", callConnectionId);
        }
    }

    public async Task SendAudioAsync(byte[] audioData)
    {
        if (_session == null) return;
        await _session.SendInputAudioAsync(BinaryData.FromBytes(audioData));
    }

    public void Close()
    {
        _session?.Dispose();
        _session = null;
    }
}
