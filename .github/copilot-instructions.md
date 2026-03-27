# Plan: ACS Voice Agent with Voice Live SDK

## TL;DR

Build a C# ASP.NET Core minimal API app that uses **ACS Call Automation** to receive incoming phone calls and bridges the call audio bidirectionally with the **Azure AI Voice Live API** via the **`Azure.AI.VoiceLive` SDK** (which connects to `gpt-4o-realtime`). Call state is managed in-process via a `ConcurrentDictionary<string, CallSession>`. The Voice Live session is configured with tools for call transfer, CRM lookup, appointment scheduling, order status, and FAQ search.

## Architecture Overview

```
Phone Call → ACS (EventGrid) → /api/incomingCall endpoint
    → AnswerCall with MediaStreaming (bidirectional WebSocket)
    → ACS Audio WebSocket (/ws) ↔ AcsMediaStreamingHandler ↔ AzureVoiceLiveService
    → VoiceLiveClient/VoiceLiveSession (Azure.AI.VoiceLive SDK)
    → gpt-4o-realtime with tools (VoiceLiveFunctionDefinition)
    → Tool calls dispatched via ToolExecutor in the session.GetUpdatesAsync() loop
```

## Call State Management

Active calls are tracked in-process using `ConcurrentDictionary<string, CallSession>` via `CallSessionManager`. Each `CallSession` holds:

| Field | Type | Purpose |
|---|---|---|
| `CallConnectionId` | `string` | ACS call identifier |
| `CorrelationId` | `string` | ACS correlation identifier |
| `CallerId` | `string` | Caller phone number |
| `AcsWebSocket` | `WebSocket` | ACS media streaming WebSocket |
| `VoiceLiveSession` | `VoiceLiveSession` | Azure.AI.VoiceLive SDK session |
| `Cts` | `CancellationTokenSource` | Per-call cancellation |
| `StartTime` | `DateTimeOffset` | When the call started |
| `ToolInvocations` | `List<ToolInvocation>` | Log of tool calls made during the call |
| `Status` | `CallStatus` | Active / Transferring / Completed |

On `CallDisconnected`: dispose VoiceLiveSession, cancel CTS, remove from dictionary.

**Why not durable orchestration?** The core of this app is a real-time bidirectional audio bridge. If the process crashes, the WebSocket, audio stream, and Voice Live session are all lost — a phone call cannot be resumed from a checkpoint. Durable orchestration adds latency and complexity with zero recovery benefit for live audio.

## Steps

### Phase 1: Project Setup

1. .NET 8 ASP.NET Core minimal API project
   - NuGet packages:
     - `Azure.AI.VoiceLive` (v1.0.0) — Voice Live SDK
     - `Azure.Communication.CallAutomation`
     - `Azure.Messaging.EventGrid`
     - `Azure.Identity`
   - `appsettings.json` with placeholder config

2. Configure `appsettings.json` with required variables:
   - `DevTunnelUri` — Dev tunnel URL for local development
   - `AcsConnectionString` — ACS resource connection string
   - `AzureVoiceLiveApiKey` — Azure AI Foundry key
   - `AzureVoiceLiveEndpoint` — Azure AI Foundry endpoint (e.g. `https://{name}.services.ai.azure.com/`)
   - `VoiceLiveModel` — Model deployment name (e.g. `gpt-4o-realtime-preview`)
   - `SystemPrompt` — Voice agent system prompt

### Phase 2: ACS Incoming Call Handler (in Program.cs)

3. `POST /api/incomingCall` — EventGrid webhook
   - Handles EventGrid subscription validation
   - Extracts `incomingCallContext` and `callerId`
   - Answers the call via `CallAutomationClient.AnswerCallAsync` with:
     - `MediaStreamingOptions`: bidirectional, mixed audio, PCM 24kHz mono, WebSocket at `/ws`
     - Callback URI at `/api/callbacks/{contextId}`

4. `POST /api/callbacks/{contextId}` — ACS callback events
   - On `CallConnected`: creates `CallSession` via `CallSessionManager`, sets `CallTransferTool.CurrentCallConnectionId`
   - On `CallDisconnected`: removes session via `CallSessionManager`
   - On `CallTransferAccepted` / `CallTransferFailed`: logs result

### Phase 3: WebSocket Audio Bridge

5. WebSocket middleware at `/ws`
   - ACS connects when media streaming starts
   - `AcsMediaStreamingHandler` wraps the ACS WebSocket for send/receive
   - Receive loop: parse ACS `AudioData` messages, extract base64 PCM audio, forward to Voice Live via `session.SendInputAudioAsync()`

6. `AzureVoiceLiveService` — bidirectional bridge using `Azure.AI.VoiceLive` SDK
   - Creates `VoiceLiveClient` with endpoint + API key
   - Starts session via `client.StartSessionAsync(sessionOptions)` with `VoiceLiveSessionOptions`
   - **Receive loop** (background task via `session.GetUpdatesAsync()`): handles strongly-typed events:
     - `SessionUpdateResponseAudioDelta` → forward `audioDelta.Delta` bytes back to ACS
     - `SessionUpdateInputAudioBufferSpeechStarted` → send StopAudio (barge-in)
     - `SessionUpdateResponseFunctionCallArgumentsDone` → dispatch tool via inline `_tools` dictionary, send result via `session.AddItemAsync(new FunctionCallOutputItem(...))`, trigger `session.StartResponseAsync()`
   - **Inbound audio**: `SendAudioAsync()` → `session.SendInputAudioAsync(BinaryData.FromBytes(audioData))`

### Phase 4: Voice Live Session Configuration

7. Session setup in `AzureVoiceLiveService.CreateSessionAsync()`:
   - Creates `VoiceLiveSessionOptions` with model, instructions, voice, audio format, VAD settings
   - Adds `VoiceLiveFunctionDefinition` tool definitions directly to `sessionOptions.Tools` (per official SDK pattern)
   - Starts session with `client.StartSessionAsync(sessionOptions)`
   - Calls `session.StartResponseAsync()` to initiate greeting

### Phase 5: Agent Tools

8. Tool definitions and dispatch are inlined in `AzureVoiceLiveService` (per official SDK pattern):
   - `VoiceLiveFunctionDefinition` added directly to `sessionOptions.Tools` in `CreateSessionAsync()`
   - Function dispatch via `switch` on function name in `HandleFunctionCallAsync()`
   - `customer_lookup` — Simulated CRM lookup by phone/ID
   - `order_status` — Simulated order tracking
   - `schedule_appointment` — Simulated book/check/cancel
   - `search_knowledge_base` — FAQ keyword search
   - `transfer_call` — Real ACS `CallConnection.TransferCallToParticipantAsync()`

### Phase 6: Wiring & DI (Program.cs)

10. Service registration:
    - `CallAutomationClient` as singleton
    - `CallSessionManager` as singleton
    - WebSocket middleware for `/ws`

## Relevant Files

- `Program.cs` — Minimal API host, DI, all endpoints, WebSocket middleware
- `Helper.cs` — EventGrid JSON parsing helpers
- `Models/CallSession.cs` — CallSession, CallStatus, ToolInvocation models
- `Services/CallSessionManager.cs` — In-process call state (`ConcurrentDictionary`)
- `Services/AcsMediaStreamingHandler.cs` — ACS WebSocket send/receive + `OutStreamingData` helpers
- `Services/AzureVoiceLiveService.cs` — Voice Live session via `Azure.AI.VoiceLive` SDK (connect, send audio, receive events, inline tool definitions & dispatch)
- `appsettings.json` — Configuration

## Verification

1. **Build verification**: `dotnet build` succeeds with no errors ✅
2. **Local startup**: `dotnet run`, verify app starts and serves at configured URL
3. **EventGrid validation**: POST EventGrid validation event to `/api/incomingCall`, confirm 200 response
4. **Incoming call test**: Use ACS phone number, call in, verify call is answered and audio bridge starts
5. **Tool invocation test**: Ask the voice agent to "look up customer 12345" and verify tool executes
6. **Call transfer test**: Ask agent to transfer the call to a specific number

## Decisions

- **Language**: C# / .NET 8
- **Hosting**: ASP.NET Core minimal API. Deploy to App Service or Container Apps (not Consumption plan — needs long-lived WebSocket connections)
- **Voice Live integration**: `Azure.AI.VoiceLive` SDK v1.0.0 (`VoiceLiveClient` / `VoiceLiveSession`) — strongly-typed events, typed tool definitions (`VoiceLiveFunctionDefinition`), no raw WebSocket management
- **Model**: Azure AI Foundry gpt-4o-realtime via Voice Live API
- **State management**: In-process `ConcurrentDictionary<string, CallSession>` — no durable orchestration
- **Audio format**: PCM 24kHz mono for both ACS and Voice Live (no resampling needed)
- **Tools are simulated**: CRM, scheduling, orders, FAQ use in-memory mock data. Call transfer uses real ACS API

## Further Considerations

1. **Audio sample rate alignment**: Both ACS and Voice Live are configured for 24kHz PCM16 mono — no resampling needed.

2. **Dev tunnel for local development**: Use Azure Dev Tunnels or ngrok to expose the local app. The tunnel URL must be set in `DevTunnelUri` in `appsettings.json` and registered as the EventGrid webhook for IncomingCall events.

3. **Post-call processing**: If post-call workflows are needed later (e.g., call summarization, CRM updates, follow-up emails), those would be good candidates for Durable Functions orchestrations — but the live call itself should remain in-process.

4. **Scaling**: Each app instance holds active calls in memory. For high call volume, use App Service (Always On) or Container Apps — not Consumption plan, to avoid cold starts and instance recycling mid-call.
