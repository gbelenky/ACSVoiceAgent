# Call Sequence: ACS Voice Agent with Voice Live SDK

This document traces the full call sequence between the PSTN caller, Azure Communication Services (ACS), the App Service (ASP.NET Core), and Azure AI Voice Live (GPT Realtime).

## Sequence Diagram

```
Caller (PSTN)          ACS                    App Service                     Voice Live (Azure AI Foundry)
     |                  |                          |                                    |
     |  1. Dial in      |                          |                                    |
     |----------------->|                          |                                    |
     |                  |  2. EventGrid            |                                    |
     |                  |  IncomingCall            |                                    |
     |                  |------------------------->|                                    |
     |                  |                          | POST /api/incomingCall              
     |                  |                          |                                    |
     |                  |  3. AnswerCallAsync      |                                    |
     |                  |    (callback + ws URIs)  |                                    |
     |                  |<-------------------------|                                    |
     |                  |                          |                                    |
     |  4. Call ringing |                          |                                    |
     |<---------------->|                          |                                    |
     |                  |                          |                                    |
     |                  |  5. CallConnected        |                                    |
     |                  |  POST /api/callbacks/{id}|                                    |
     |                  |------------------------->|                                    |
     |                  |                          | 6. CreateSession()                 |
     |                  |                          |    ConcurrentDict + Channel.Write  |
     |                  |                          |                                    |
     |                  |  7. WebSocket /ws        |                                    |
     |                  |  (ACS opens WS)          |                                    |
     |                  |=========================>|                                    |
     |                  |                          | 8. AcceptWebSocketAsync()          |
     |                  |                          |    AcsMediaStreamingHandler created|
     |                  |                          |    AzureVoiceLiveService created   |
     |                  |                          |                                    |
     |                  |                          | 9. WaitForSessionAsync()           |
     |                  |                          |    Channel.ReadAsync (gets session)|
     |                  |                          |    Binds ACS WebSocket to session  |
     |                  |                          |                                    |
     |                  |                          | 10. StartAsync(callSession)        |
     |                  |                          |     → CreateSessionAsync()         |
     |                  |                          |                                    |
     |                  |                          | 11. VoiceLiveClient                |
     |                  |                          |     .StartSessionAsync(options)    |
     |                  |                          |----------------------------------->|
     |                  |                          |     (model, voice, 8 tools,        |
     |                  |                          |      VAD, noise reduction,         |
     |                  |                          |      echo cancellation)            |
     |                  |                          |                                    |
     |                  |                          |  12. Session established           |
     |                  |                          |<-----------------------------------|
     |                  |                          |                                    |
     |                  |                          | 13. Task.Run(ReceiveEventsAsync)   |
     |                  |                          |     Background event loop starts   |
     |                  |                          |                                    |
     |                  |                          | 14. session.StartResponseAsync()   |
     |                  |                          |----------------------------------->|
     |                  |                          |     (triggers greeting)            |
     |                  |                          |                                    |
     |                  |                          |  15. AudioDelta events (greeting)  |
     |                  |                          |<-----------------------------------|
     |                  |                          |                                    |
     |                  |  16. AudioData (to caller)|                                   |
     |                  |<=========================|                                    |
     |  17. Hears       |                          | OutStreamingData                   |
     |      greeting    |                          | .GetAudioDataForOutbound()         |
     |<-----------------|                          |                                    |
     |                  |                          |                                    |
     |  18. Caller      |                          |                                    |
     |      speaks      |                          |                                    |
     |----------------->|                          |                                    |
     |                  |  19. AudioData (from      |                                   |
     |                  |      caller) via WS       |                                   |
     |                  |=========================>|                                    |
     |                  |                          | 20. Parse AudioData JSON           |
     |                  |                          |     Base64 decode                  |
     |                  |                          |     SendAudioAsync(bytes)          |
     |                  |                          |----------------------------------->|
     |                  |                          |     session.SendInputAudioAsync()  |
     |                  |                          |                                    |
     |                  |                          |  21. VAD detects end of speech     |
     |                  |                          |      Model processes + responds    |
     |                  |                          |                                    |
     |                  |                          |  22. AudioDelta (response audio)   |
     |                  |                          |<-----------------------------------|
     |                  |  AudioData (response)     |                                   |
     |                  |<=========================|                                    |
     |  Hears response  |                          |                                    |
     |<-----------------|                          |                                    |
     |                  |                          |                                    |
```

## Barge-In (Interruption)

```
Caller (PSTN)          ACS                    App Service                     Voice Live
     |                  |                          |                                    |
     |  Caller speaks   |                          |  (while agent is still speaking)   |
     |  during agent    |                          |                                    |
     |  response        |                          |                                    |
     |----------------->|=========================>|----------------------------------->|
     |                  |                          |                                    |
     |                  |                          |  SpeechStarted event               |
     |                  |                          |<-----------------------------------|
     |                  |                          |                                    |
     |                  |  StopAudio               |                                    |
     |                  |<=========================|  (stops playback immediately)      |
     |                  |                          |                                    |
```

## Tool Call Flow (e.g., customer_lookup)

```
Voice Live                    App Service                        AgentFunctions
     |                              |                                  |
     | FunctionCallArgumentsDone    |                                  |
     | (name, callId, arguments)    |                                  |
     |----------------------------->|                                  |
     |                              | HandleFunctionCallAsync()        |
     |                              | Parse args, switch on name       |
     |                              |--------------------------------->|
     |                              |              LookupCustomer(args)|
     |                              |<---------------------------------|
     |                              |              JSON result         |
     |                              |                                  |
     |  FunctionCallOutputItem      |                                  |
     |  (callId, result)            |                                  |
     |<-----------------------------|                                  |
     |                              |                                  |
     |  StartResponseAsync()        |                                  |
     |<-----------------------------|                                  |
     |                              |                                  |
     | AudioDelta (speaks result)   |                                  |
     |----------------------------->|                                  |
     |                              | → ACS → Caller hears answer      |
```

## End Call Flow

```
Voice Live                    App Service                        ACS
     |                              |                              |
     | FunctionCallArgumentsDone    |                              |
     | (end_call, reason)           |                              |
     |----------------------------->|                              |
     |                              | EndCallAsync() returns JSON  |
     |                              | _endCallRequested = true     |
     |                              |                              |
     |  FunctionCallOutputItem +    |                              |
     |  StartResponseAsync()        |                              |
     |<-----------------------------|                              |
     |                              |                              |
     | AudioDelta (goodbye audio)   |                              |
     |----------------------------->| → ACS → Caller hears goodbye |
     |                              |                              |
     | ResponseDone event           |                              |
     |----------------------------->|                              |
     |                              | HangUpAfterDelayAsync()      |
     |                              | (2s delay for audio flush)   |
     |                              |                              |
     |                              | callConnection.HangUpAsync() |
     |                              |----------------------------->|
     |                              |                              |
     |                              |  CallDisconnected event      |
     |                              |<-----------------------------|
     |                              |  RemoveSession()             |
     |                              |  Dispose VoiceLive, cancel   |
```

## Call Transfer Flow

```
Voice Live                    App Service                        ACS
     |                              |                              |
     | FunctionCallArgumentsDone    |                              |
     | (transfer_call, reason)      |                              |
     |----------------------------->|                              |
     |                              | TransferCallAsync()          |
     |                              | TransferCallToParticipant    |
     |                              |   Async(target phone)        |
     |                              |----------------------------->|
     |                              |                              |
     |                              |  CallTransferAccepted        |
     |                              |  (or CallTransferFailed)     |
     |                              |<-----------------------------|
```

## Code Locations

| Step | Code Location | Method/Line |
|------|--------------|-------------|
| 2 | `Program.cs` | `POST /api/incomingCall` — EventGrid handler |
| 3 | `Program.cs` | `client.AnswerCallAsync(options)` |
| 5-6 | `Program.cs` | `POST /api/callbacks/{contextId}` → `sessionManager.CreateSession()` |
| 6 | `Services/CallSessionManager.cs` | `CreateSession()` — adds to ConcurrentDict + Channel.Write |
| 7-8 | `Program.cs` | WebSocket middleware at `/ws` → `HandleWebSocket()` |
| 9 | `Program.cs` | `sessionManager.WaitForSessionAsync()` — Channel.ReadAsync |
| 10-11 | `Services/AzureVoiceLiveService.cs` | `StartAsync()` → `CreateSessionAsync()` |
| 11 | `Services/AzureVoiceLiveService.cs` | `client.StartSessionAsync(sessionOptions)` |
| 13-14 | `Services/AzureVoiceLiveService.cs` | `Task.Run(ReceiveEventsAsync)` + `session.StartResponseAsync()` |
| 15-16 | `Services/AzureVoiceLiveService.cs` | `HandleSessionUpdateAsync` → `SessionUpdateResponseAudioDelta` case |
| 16 | `Services/AcsMediaStreamingHandler.cs` | `SendMessageAsync()` via `OutStreamingData.GetAudioDataForOutbound()` |
| 19-20 | `Program.cs` | Audio receive loop — parse `AudioData` JSON, base64 decode |
| 20 | `Services/AzureVoiceLiveService.cs` | `SendAudioAsync()` → `session.SendInputAudioAsync()` |
| Barge-in | `Services/AzureVoiceLiveService.cs` | `SessionUpdateInputAudioBufferSpeechStarted` → `StopAudio` |
| Tool call | `Services/AzureVoiceLiveService.cs` | `HandleFunctionCallAsync()` → `AgentFunctions.*` |
| End call | `Services/AzureVoiceLiveService.cs` | `EndCallAsync()` + `HangUpAfterDelayAsync()` |
| Transfer | `Services/AzureVoiceLiveService.cs` | `TransferCallAsync()` |
| Cleanup | `Services/CallSessionManager.cs` | `RemoveSession()` — dispose + cancel + remove |

## Audio Format

All audio flows use **PCM 24kHz 16-bit mono** — no resampling needed between ACS and Voice Live.

- ACS → App Service: WebSocket JSON with base64-encoded PCM (`AudioData.data`)
- App Service → Voice Live: Raw PCM bytes via `session.SendInputAudioAsync()`
- Voice Live → App Service: PCM bytes via `SessionUpdateResponseAudioDelta.Delta`
- App Service → ACS: WebSocket JSON with base64-encoded PCM (`OutStreamingData.GetAudioDataForOutbound()`)
