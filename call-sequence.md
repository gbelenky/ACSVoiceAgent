# Call Sequence: ACS Voice Agent with Foundry Agent + Voice Live

This document traces the full call sequence between the PSTN caller, Azure Communication Services (ACS), the App Service (ASP.NET Core), and Azure AI Voice Live with a Foundry Agent (GPT Realtime + file_search + function tools).

## Infrastructure Provisioning (`azd up`)

```
Developer                azd CLI                 Bicep/ARM                   Python (agent_manager.py)
     |                      |                        |                              |
     | 1. azd up            |                        |                              |
     |--------------------->|                        |                              |
     |                      | 2. azd provision       |                              |
     |                      |----------------------->|                              |
     |                      |                        | 3. Create Resource Group     |
     |                      |                        | 4. AI Services (S0)          |
     |                      |                        |    + gpt-4.1-mini deployment |
     |                      |                        | 5. Foundry Project           |
     |                      |                        |    (voiceAgentProject)       |
     |                      |                        | 6. RBAC: Azure AI User       |
     |                      |                        |    - project identity        |
     |                      |                        |    - developer identity      |
     |                      |                        |    - App Service identity    |
     |                      |                        | 7. App Service (B1 Linux)    |
     |                      |                        |    + SystemAssigned identity |
     |                      |                        |    + App Insights + Dashboard|
     |                      |                        |                              |
     |                      | 8. postprovision hook  |                              |
     |                      |  (azure.yaml)          |                              |
     |                      |-------------------------------------------------->|
     |                      |                        |                              |
     |                      |                        |              9. Upload        |
     |                      |                        |  product-catalog.md to Files  |
     |                      |                        |                              |
     |                      |                        |             10. Create       |
     |                      |                        |    ProductCatalog            |
     |                      |                        |         (vector store)       |
     |                      |                        |                              |
     |                      |                        |             11. Create       |
     |                      |                        |    VoiceLiveAgent (Standard) |
     |                      |                        |    - 8x FunctionTool         |
     |                      |                        |      (client-side dispatch)  |
     |                      |                        |    - FileSearchTool          |
     |                      |                        |      (product catalog)       |
     |                      |                        |    - Instructions from       |
     |                      |                        |      system-prompt.txt       |
     |                      |                        |                              |
     |                      |<--------------------------------------------------|
     |                      |  AGENT_ID, VECTORSTORE_ID, FILE_ID saved to azd env  |
     |                      |                        |                              |
     |                      | 12. az webapp config   |                              |
     |                      |  set FoundryAgentVersion                              |
     |                      |                        |                              |
     |                      | 13. azd deploy         |                              |
     |                      |  (C# app to App Service)                              |
     |                      |                        |                              |
```

## Call Flow

```
Caller (PSTN)          ACS                    App Service                     Voice Live + Foundry Agent
     |                  |                          |                                    |
     |  1. Dial in      |                          |                                    |
     |----------------->|                          |                                    |
     |                  |  2. EventGrid            |                                    |
     |                  |  IncomingCall            |                                    |
     |                  |------------------------->|                                    |
     |                  |                          | POST /api/incomingCall              |
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
     |                  |                          |     -> CreateSessionAsync()        |
     |                  |                          |                                    |
     |                  |                          | 11. VoiceLiveClient                |
     |                  |                          |     (Entra ID / DefaultAzureCredential)
     |                  |                          |     .StartSessionAsync(            |
     |                  |                          |       SessionTarget.FromAgent(     |
     |                  |                          |         AgentSessionConfig))       |
     |                  |                          |----------------------------------->|
     |                  |                          |     Agent loaded with:             |
     |                  |                          |     - instructions                 |
     |                  |                          |     - 8 function tools             |
     |                  |                          |       (client-side dispatch)       |
     |                  |                          |     - file_search                  |
     |                  |                          |       (product catalog)            |
     |                  |                          |                                    |
     |                  |                          | 12. ConfigureSessionAsync(options) |
     |                  |                          |     PCM16 input/output format      |
     |                  |                          |----------------------------------->|
     |                  |                          |                                    |
     |                  |                          |  13. Session established           |
     |                  |                          |<-----------------------------------|
     |                  |                          |                                    |
     |                  |                          | 14. Task.Run(ReceiveEventsAsync)   |
     |                  |                          |     Background event loop starts   |
     |                  |                          |                                    |
     |                  |                          | 15. SendCommandAsync               |
     |                  |                          |     (conversation.item.create +    |
     |                  |                          |      response.create)              |
     |                  |                          |----------------------------------->|
     |                  |                          |     (triggers greeting)            |
     |                  |                          |                                    |
     |                  |                          |  16. AudioDelta events (greeting)  |
     |                  |                          |<-----------------------------------|
     |                  |                          |                                    |
     |                  |  17. AudioData (to caller)|                                   |
     |                  |<=========================|                                    |
     |  18. Hears       |                          | OutStreamingData                   |
     |      greeting    |                          | .GetAudioDataForOutbound()         |
     |<-----------------|                          |                                    |
     |                  |                          |                                    |
     |  19. Caller      |                          |                                    |
     |      speaks      |                          |                                    |
     |----------------->|                          |                                    |
     |                  |  20. AudioData (from      |                                   |
     |                  |      caller) via WS       |                                   |
     |                  |=========================>|                                    |
     |                  |                          | 21. Parse AudioData JSON           |
     |                  |                          |     Base64 decode                  |
     |                  |                          |     SendAudioAsync(bytes)          |
     |                  |                          |----------------------------------->|
     |                  |                          |     session.SendInputAudioAsync()  |
     |                  |                          |                                    |
     |                  |                          |  22. VAD detects end of speech     |
     |                  |                          |      Model processes + responds    |
     |                  |                          |                                    |
     |                  |                          |  23. AudioDelta (response audio)   |
     |                  |                          |<-----------------------------------|
     |                  |  AudioData (response)     |                                   |
     |                  |<=========================|                                    |
     |  Hears response  |                          |                                    |
     |<-----------------|                          |                                    |
     |                  |                          |                                    |
```

## Barge-In (Interruption)

```
Caller (PSTN)          ACS                    App Service                     Voice Live + Foundry Agent
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

## Product Catalog Query (file_search — server-side)

```
Voice Live + Foundry Agent                App Service
     |                                         |
     | (Model decides to use file_search)      |
     | Searches vector store                   |
     |   (product-catalog.md)                  |
     | Retrieves product info, specs, pricing  |
     |                                         |
     | AudioDelta (speaks answer to caller)    |
     |---------------------------------------->|
     |                                         | -> ACS -> Caller hears answer
     |                                         |
```

No client-side code is needed. The Foundry Agent's FileSearchTool queries the
vector store and the model incorporates the results directly into its response.
This is the NEW capability added by the Foundry Agent — the main branch cannot do this.

## Function Tool Call Flow (client-side dispatch)

```
Voice Live + Foundry Agent          App Service                        AgentFunctions
     |                                    |                                  |
     | FunctionCallArgumentsDone          |                                  |
     | (name, callId, arguments)          |                                  |
     |----------------------------------->|                                  |
     |                                    | HandleFunctionCallAsync()        |
     |                                    | Parse args, switch on name       |
     |                                    |--------------------------------->|
     |                                    |              e.g. LookupCustomer |
     |                                    |<---------------------------------|
     |                                    |              JSON result         |
     |                                    |                                  |
     |  FunctionCallOutputItem            |                                  |
     |  (callId, result)                  |                                  |
     |<-----------------------------------|                                  |
     |                                    |                                  |
     |  StartResponseAsync()              |                                  |
     |<-----------------------------------|                                  |
     |                                    |                                  |
     | AudioDelta (speaks result)         |                                  |
     |----------------------------------->|                                  |
     |                                    | -> ACS -> Caller hears answer    |
```

## ACS Tool Call Flow (transfer_call / end_call — client-side)

```
Voice Live + Foundry Agent          App Service                        ACS
     |                                    |                              |
     | FunctionCallArgumentsDone          |                              |
     | (name, callId, arguments)          |                              |
     |----------------------------------->|                              |
     |                                    | HandleFunctionCallAsync()    |
     |                                    | Parse args, switch on name   |
     |                                    |                              |
     |                                    |  (ACS API call)              |
     |                                    |----------------------------->|
     |                                    |                              |
     |  FunctionCallOutputItem            |                              |
     |  (callId, result)                  |                              |
     |<-----------------------------------|                              |
     |                                    |                              |
     |  StartResponseAsync()              |                              |
     |<-----------------------------------|                              |
     |                                    |                              |
     | AudioDelta (speaks result)         |                              |
     |----------------------------------->|                              |
     |                                    | -> ACS -> Caller hears       |
```

## End Call Flow

```
Voice Live + Foundry Agent          App Service                        ACS
     |                                    |                              |
     | FunctionCallArgumentsDone          |                              |
     | (end_call, reason)                 |                              |
     |----------------------------------->|                              |
     |                                    | Returns "Call ending."       |
     |                                    | Schedules HangUp (6s delay) |
     |                                    |                              |
     |  FunctionCallOutputItem +          |                              |
     |  StartResponseAsync()              |                              |
     |<-----------------------------------|                              |
     |                                    |                              |
     | AudioDelta (goodbye audio)         |                              |
     |----------------------------------->| -> ACS -> Caller hears bye   |
     |                                    |                              |
     |                                    | (after 6s delay)             |
     |                                    | callConnection.HangUpAsync() |
     |                                    |----------------------------->|
     |                                    |                              |
     |                                    |  CallDisconnected event      |
     |                                    |<-----------------------------|
     |                                    |  RemoveSession()             |
     |                                    |  Dispose VoiceLive, cancel   |
```

## Call Transfer Flow

```
Voice Live + Foundry Agent          App Service                        ACS
     |                                    |                              |
     | FunctionCallArgumentsDone          |                              |
     | (transfer_call, phone, reason)     |                              |
     |----------------------------------->|                              |
     |                                    | TransferCallToParticipant    |
     |                                    |   Async(target phone)        |
     |                                    |----------------------------->|
     |                                    |                              |
     |                                    |  CallTransferAccepted        |
     |                                    |  (or CallTransferFailed)     |
     |                                    |<-----------------------------|
```

## Teardown (`azd down`)

```
Developer                azd CLI                 Python (agent_manager.py)        Bicep/ARM
     |                      |                              |                        |
     | 1. azd down          |                              |                        |
     |--------------------->|                              |                        |
     |                      | 2. predown hook              |                        |
     |                      |----------------------------->|                        |
     |                      |                              | 3. Delete agent        |
     |                      |                              | 4. Delete vector store |
     |                      |                              | 5. Delete file         |
     |                      |<-----------------------------|                        |
     |                      |                              |                        |
     |                      | 6. Delete Azure resources    |                        |
     |                      |---------------------------------------------->|
     |                      |                              |                        |
```

## Tool Architecture

| Tool | Type | Where it runs | Data source |
|------|------|---------------|-------------|
| `file_search` | FileSearchTool | Server-side (Foundry Agent) | Vector store over `product-catalog.md` |
| `customer_lookup` | FunctionTool | Client-side (C# App Service) | `AgentFunctions.LookupCustomer()` |
| `order_status` | FunctionTool | Client-side (C# App Service) | `AgentFunctions.CheckOrderStatus()` |
| `check_appointment` | FunctionTool | Client-side (C# App Service) | `AgentFunctions.CheckAppointment()` |
| `book_appointment` | FunctionTool | Client-side (C# App Service) | `AgentFunctions.BookAppointment()` |
| `cancel_appointment` | FunctionTool | Client-side (C# App Service) | `AgentFunctions.CancelAppointment()` |
| `search_knowledge_base` | FunctionTool | Client-side (C# App Service) | `AgentFunctions.SearchKnowledgeBase()` |
| `transfer_call` | FunctionTool | Client-side (C# App Service) | ACS `TransferCallToParticipantAsync` |
| `end_call` | FunctionTool | Client-side (C# App Service) | ACS `HangUpAsync` (6s delay) |

## Code Locations

| Step | Code Location | Method/Line |
|------|--------------|-------------|
| Infra | `infra/main.bicep` | AI Services + Foundry Project + RBAC + App Service |
| Agent creation | `scripts/agent_manager.py` | `create_agent()` — 8 FunctionTools + FileSearchTool |
| Product catalog | `scripts/docs/product-catalog.md` | Product specs, pricing, availability (vector store) |
| Business logic | `Services/AgentFunctions.cs` | Customer, order, appointment, FAQ lookups |
| 2 | `Program.cs` | `POST /api/incomingCall` — EventGrid handler |
| 3 | `Program.cs` | `client.AnswerCallAsync(options)` |
| 5-6 | `Program.cs` | `POST /api/callbacks/{contextId}` -> `sessionManager.CreateSession()` |
| 6 | `Services/CallSessionManager.cs` | `CreateSession()` — ConcurrentDict + Channel.Write |
| 7-8 | `Program.cs` | WebSocket middleware at `/ws` -> `HandleWebSocket()` |
| 9 | `Program.cs` | `sessionManager.WaitForSessionAsync()` — Channel.ReadAsync |
| 10-11 | `Services/AzureVoiceLiveService.cs` | `StartAsync()` -> `CreateSessionAsync()` |
| 11 | `Services/AzureVoiceLiveService.cs` | `SessionTarget.FromAgent(AgentSessionConfig)` |
| 12 | `Services/AzureVoiceLiveService.cs` | `session.ConfigureSessionAsync(options)` |
| 14-15 | `Services/AzureVoiceLiveService.cs` | `Task.Run(ReceiveEventsAsync)` + `SendProactiveGreetingAsync()` |
| 16-17 | `Services/AzureVoiceLiveService.cs` | `SessionUpdateResponseAudioDelta` case |
| 17 | `Services/AcsMediaStreamingHandler.cs` | `SendMessageAsync()` via `OutStreamingData.GetAudioDataForOutbound()` |
| 20-21 | `Program.cs` | Audio receive loop — parse `AudioData` JSON, base64 decode |
| 21 | `Services/AzureVoiceLiveService.cs` | `SendAudioAsync()` -> `session.SendInputAudioAsync()` |
| Barge-in | `Services/AzureVoiceLiveService.cs` | `SessionUpdateInputAudioBufferSpeechStarted` -> `StopAudio` |
| Function tools | `Services/AzureVoiceLiveService.cs` | `HandleFunctionCallAsync()` -> `AgentFunctions.*` + ACS APIs |
| Cleanup | `Services/CallSessionManager.cs` | `RemoveSession()` — dispose + cancel + remove |

## Audio Format

All audio flows use **PCM 24kHz 16-bit mono** — no resampling needed between ACS and Voice Live.

- ACS -> App Service: WebSocket JSON with base64-encoded PCM (`AudioData.data`)
- App Service -> Voice Live: Raw PCM bytes via `session.SendInputAudioAsync()`
- Voice Live -> App Service: PCM bytes via `SessionUpdateResponseAudioDelta.Delta`
- App Service -> ACS: WebSocket JSON with base64-encoded PCM (`OutStreamingData.GetAudioDataForOutbound()`)
