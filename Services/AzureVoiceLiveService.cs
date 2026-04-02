using System.Text.Json;
using Azure.AI.VoiceLive;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Identity;
using ACSVoiceAgent.Models;

namespace ACSVoiceAgent.Services;

/// <summary>
/// Voice Live service using Foundry Agent mode.
/// Tools, instructions, and voice config are managed in the Foundry Agent — not in code.
/// This service only handles the audio bridge and event loop.
/// </summary>
public class AzureVoiceLiveService
{
    private readonly AcsMediaStreamingHandler _mediaStreaming;
    private readonly CallAutomationClient _callClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureVoiceLiveService> _logger;
    private VoiceLiveSession? _session;
    private CallSession? _callSession;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _holdAudioCts;
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

        // Play hold audio while Voice Live connects (6-14s warm, 30s+ cold)
        _holdAudioCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        _ = PlayHoldAudioAsync(_holdAudioCts.Token);

        await CreateSessionAsync();
    }

    private async Task CreateSessionAsync()
    {
        var endpoint = _configuration.GetValue<string>("VoiceLiveEndpoint");
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);

        var agentName = _configuration.GetValue<string>("FoundryAgentName");
        ArgumentNullException.ThrowIfNullOrEmpty(agentName);

        var projectName = _configuration.GetValue<string>("FoundryProjectName");
        ArgumentNullException.ThrowIfNullOrEmpty(projectName);

        var agentVersion = _configuration.GetValue<string>("FoundryAgentVersion");

        // Build agent session config
        var agentConfig = new AgentSessionConfig(agentName, projectName);
        if (!string.IsNullOrEmpty(agentVersion))
        {
            agentConfig.AgentVersion = agentVersion;
        }

        // Agent mode requires Entra ID authentication (no API key)
        var client = new VoiceLiveClient(new Uri(endpoint), new DefaultAzureCredential());

        _logger.LogInformation(
            "Connecting to Voice Live at {Endpoint} with Foundry Agent '{AgentName}' in project '{ProjectName}'...",
            endpoint, agentName, projectName);

        // Connect using SessionTarget.FromAgent — the agent defines tools, instructions, and voice config
        _session = await client.StartSessionAsync(
            SessionTarget.FromAgent(agentConfig), _cts!.Token);

        if (_callSession != null)
        {
            _callSession.VoiceLiveSession = _session;
        }

        // Configure session options (audio format for ACS compatibility)
        var options = new VoiceLiveSessionOptions
        {
            InputAudioFormat = InputAudioFormat.Pcm16,
            OutputAudioFormat = OutputAudioFormat.Pcm16,
        };

        await _session.ConfigureSessionAsync(options, _cts.Token);

        _logger.LogInformation("Connected to Voice Live with Foundry Agent successfully");

        _ = Task.Run(() => ReceiveEventsAsync(_cts.Token));
    }

    private async Task PlayHoldAudioAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Playing hold audio while connecting to Voice Live");
        try
        {
            // Ring-back tone: 440Hz, 1s on / 3s off, PCM 16-bit 24kHz mono
            const int sampleRate = 24000;
            const double frequency = 440.0;
            const double amplitude = 1000.0; // Gentle volume (max ~32767)
            const int toneMs = 1000;
            const int silenceMs = 3000;

            var toneSamples = sampleRate * toneMs / 1000;
            var toneBytes = new byte[toneSamples * 2]; // 16-bit = 2 bytes/sample
            for (int i = 0; i < toneSamples; i++)
            {
                var sample = (short)(amplitude * Math.Sin(2 * Math.PI * frequency * i / sampleRate));
                toneBytes[i * 2] = (byte)(sample & 0xFF);
                toneBytes[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            var silenceBytes = new byte[sampleRate * silenceMs / 1000 * 2];

            // Send in ~100ms chunks to avoid large frames
            const int chunkSize = 24000 * 2 / 10; // 100ms of audio = 4800 bytes

            while (!cancellationToken.IsCancellationRequested)
            {
                await SendChunkedAudioAsync(toneBytes, chunkSize, cancellationToken);
                await SendChunkedAudioAsync(silenceBytes, chunkSize, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Hold audio stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hold audio playback error");
        }
    }

    private async Task SendChunkedAudioAsync(byte[] audio, int chunkSize, CancellationToken cancellationToken)
    {
        for (int offset = 0; offset < audio.Length; offset += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var length = Math.Min(chunkSize, audio.Length - offset);
            var chunk = new byte[length];
            Buffer.BlockCopy(audio, offset, chunk, 0, length);
            var outbound = OutStreamingData.GetAudioDataForOutbound(chunk);
            await _mediaStreaming.SendMessageAsync(outbound);
            // Pace sending to roughly real-time (100ms per chunk)
            await Task.Delay(95, cancellationToken);
        }
    }

    private void StopHoldAudio()
    {
        if (_holdAudioCts != null && !_holdAudioCts.IsCancellationRequested)
        {
            _logger.LogInformation("Stopping hold audio");
            _holdAudioCts.Cancel();
            _holdAudioCts.Dispose();
            _holdAudioCts = null;
        }
    }

    private async Task SendProactiveGreetingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending proactive greeting request");
        try
        {
            await _session!.SendCommandAsync(
                BinaryData.FromObjectAsJson(new
                {
                    type = "conversation.item.create",
                    item = new
                    {
                        type = "message",
                        role = "system",
                        content = new[]
                        {
                            new { type = "input_text", text = "Greet the caller and introduce yourself." }
                        }
                    }
                }), cancellationToken);

            await _session!.SendCommandAsync(
                BinaryData.FromObjectAsJson(new { type = "response.create" }),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send proactive greeting");
        }
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
                StopHoldAudio();
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

            case SessionUpdateResponseAudioTranscriptDone transcriptDone:
                _logger.LogInformation("Agent transcript: {Transcript}", transcriptDone.Transcript);
                _callSession?.Transcript.Add(new TranscriptEntry
                {
                    Speaker = "Agent",
                    Text = transcriptDone.Transcript
                });
                break;

            case SessionUpdateConversationItemInputAudioTranscriptionCompleted transcription:
                _logger.LogInformation("User transcript: {Transcript}", transcription.Transcript);
                _callSession?.Transcript.Add(new TranscriptEntry
                {
                    Speaker = "User",
                    Text = transcription.Transcript
                });
                break;

            case SessionUpdateResponseFunctionCallArgumentsDone functionCall:
                // Function tools are dispatched client-side.
                // file_search (product catalog) is handled server-side by the Foundry Agent.
                await HandleFunctionCallAsync(functionCall.Name, functionCall.CallId,
                    functionCall.Arguments, cancellationToken);
                break;

            case SessionUpdateResponseAudioDone:
                _logger.LogDebug("Audio response completed");
                break;

            case SessionUpdateResponseDone:
                _logger.LogDebug("Response completed");
                break;

            case SessionUpdateError errorEvent:
                var errorMsg = errorEvent.Error?.Message;
                var errorCode = errorEvent.Error?.Code;
                if (errorMsg?.Contains("Cancellation failed: no active response") == true
                    || errorCode == "conversation_already_has_active_response")
                {
                    // Benign errors during barge-in or greeting overlap
                    _logger.LogDebug("Benign Voice Live error: {Code} - {Message}", errorCode, errorMsg);
                }
                else
                {
                    _logger.LogWarning("Voice Live error: {Code} - {Message}",
                        errorCode, errorMsg);
                }
                break;

            default:
                _logger.LogDebug("Voice Live event: {EventType}", serverEvent.GetType().Name);
                break;
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
            _logger.LogWarning("EndCallAsync: No active call connection ID");
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
        await Task.Delay(6000);

        _logger.LogWarning("Hanging up call {CallConnectionId}", callConnectionId);
        try
        {
            var callConnection = _callClient.GetCallConnection(callConnectionId);
            await callConnection.HangUpAsync(true);
            _logger.LogWarning("HangUpAsync completed successfully for {CallConnectionId}", callConnectionId);
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
