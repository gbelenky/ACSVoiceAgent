# ACS Voice Agent with Azure AI Foundry Agent + Voice Live SDK

A C# ASP.NET Core minimal API that bridges **Azure Communication Services (ACS)** incoming phone calls with the **Azure AI Voice Live API** via a **Foundry Agent** (`gpt-realtime-mini`) for real-time AI-powered voice conversations. The Foundry Agent manages tools, instructions, and voice configuration — the C# app handles the audio bridge and client-side function dispatch.

## Architecture

```
Phone Call → ACS (EventGrid) → /api/incomingCall
    → AnswerCall with bidirectional MediaStreaming
    → ACS Audio WebSocket (/ws) ↔ AcsMediaStreamingHandler ↔ AzureVoiceLiveService
    → VoiceLiveClient with SessionTarget.FromAgent (Entra ID auth)
    → Foundry Agent (gpt-realtime-mini + file_search + 8 function tools)
    → Function tools dispatched client-side via AgentFunctions.cs
    → file_search (product catalog) handled server-side by Foundry Agent
```

### Hybrid Architecture

This branch uses a **hybrid approach** where the Foundry Agent is the control plane and the C# app is the execution plane:

| Layer | Responsibility | Where |
|---|---|---|
| **Tool definitions** | All 9 tools defined in Foundry Agent metadata | `agent_manager.py` → Foundry |
| **Instructions / system prompt** | Loaded into Foundry Agent at creation | `Prompts/system-prompt.txt` |
| **Voice config** | Audio format configured in session options | `AzureVoiceLiveService.cs` |
| **Function dispatch** (8 tools) | Executed client-side in C# event loop | `AgentFunctions.cs` |
| **file_search** (product catalog) | Executed server-side by Foundry Agent | Vector store in Foundry |
| **Audio bridge** | Bidirectional ACS ↔ Voice Live | `AzureVoiceLiveService.cs` |

## Agent Tools

| Tool | Type | Dispatch | Description |
|---|---|---|---|
| `customer_lookup` | Function | Client-side (C#) | Look up customer info by phone number or customer ID |
| `order_status` | Function | Client-side (C#) | Check order tracking status |
| `check_appointment` | Function | Client-side (C#) | Check existing appointment details |
| `book_appointment` | Function | Client-side (C#) | Book a new appointment |
| `cancel_appointment` | Function | Client-side (C#) | Cancel an existing appointment |
| `search_knowledge_base` | Function | Client-side (C#) | Search FAQ/knowledge base by keyword |
| `transfer_call` | Function | Client-side (C#) | Transfer the call to a human agent via ACS |
| `end_call` | Function | Client-side (C#) | End the current call and disconnect |
| `file_search` | File Search | Server-side (Foundry) | Search the product catalog (vector store) |

**Client-side tools** use simulated in-memory data (defined in `AgentFunctions.cs`) except `transfer_call` and `end_call`, which perform real ACS operations. **Server-side `file_search`** is handled entirely by the Foundry Agent using a vector store over `product-catalog.md`.

## Voice & Audio Configuration

| Setting | Value | Notes |
|---|---|---|
| **Voice** | `en-US-Ava:DragonHDLatestNeural` | Azure HD voice via `AzureStandardVoice` |
| **Input audio** | PCM 16-bit, 24kHz mono | From ACS media streaming |
| **Output audio** | PCM 16-bit | Back to ACS |
| **Noise reduction** | `FarField` | Server-side noise suppression for phone calls |
| **Echo cancellation** | Enabled | Prevents AI voice feedback loop |

### Voice Activity Detection (VAD)

Server-side VAD (`ServerVadTurnDetection`) controls how the model decides when the caller has finished speaking:

| Parameter | Default | Description |
|---|---|---|
| **`Threshold`** | `0.5` | Sensitivity (0.0–1.0). Higher = requires louder speech, lower = picks up quieter speech but more noise |
| **`PrefixPadding`** | `300ms` | Audio kept before speech is detected — prevents clipping the start of words |
| **`SilenceDuration`** | `500ms` | How long the caller must be silent before the model considers their turn complete |

**Tuning tips:**
- Agent interrupts too often → increase `SilenceDuration` to 700–800ms
- Agent is too slow to respond → decrease `SilenceDuration` to 300–400ms
- Misses quiet speakers → lower `Threshold` to 0.3
- Picks up too much background noise → raise `Threshold` to 0.6–0.7

### Noise Reduction

Server-side noise reduction is configured via `AudioNoiseReductionType`. Available modes:

| Mode | Best For | Description |
|---|---|---|
| **`FarField`** | Phone calls, speakerphone | Optimized for far-field audio where the microphone is away from the speaker. **Current setting.** |
| **`NearField`** | Headset, handset held to ear | Optimized for near-field audio where the microphone is close to the speaker's mouth |
| **`AzureDeepNoiseSuppression`** | Very noisy environments | Most aggressive suppression — best when background noise is severe, but may affect voice naturalness |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Developer CLI (azd)](https://aka.ms/azd-install) — provisions infrastructure + creates the Foundry Agent
- [Azure CLI (az)](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) — used by azd hooks
- [Python 3.10+](https://www.python.org/) and [uv](https://docs.astral.sh/uv/) — for the agent manager script
- An **Azure Communication Services** resource with a phone number configured for incoming calls
- An **Azure subscription** in a [supported region](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#model-summary-table-and-region-availability) for `gpt-4.1-mini` (`eastus2`, `swedencentral`, `australiaeast`, or `northcentralus`)
- A tunneling tool for local development ([Azure Dev Tunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) or [ngrok](https://ngrok.com/))

> **Note**: Unlike the `main` branch, this branch uses **Entra ID authentication** (via `DefaultAzureCredential`) — no API keys needed. The `azd up` flow provisions the AI Services resource, Foundry Project, model deployment, and RBAC automatically.

## Local Setup

### 1. Clone and restore

```bash
cd ACSVoiceAgent
dotnet restore
```

### 2. Create a dev tunnel

Using Azure Dev Tunnels:

```bash
devtunnel create --allow-anonymous
devtunnel port create -p 5000
devtunnel host
```

Copy the tunnel URL (e.g. `https://abc123.devtunnels.ms`).

### 3. Configure settings

For local development, fill in your values in `appsettings.Development.json` (this file is loaded automatically when running in the `Development` environment and should **not** be committed to source control):

```json
{
  "DevTunnelUri": "https://<your-tunnel-url>",
  "AcsConnectionString": "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>",
  "VoiceLiveEndpoint": "https://<your-ai-services>.cognitiveservices.azure.com",
  "FoundryAgentName": "VoiceLiveAgent",
  "FoundryProjectName": "voiceAgentProject",
  "FoundryAgentVersion": "<version-from-azd-up>"
}
```

| Setting | Description |
|---|---|
| `DevTunnelUri` | Your dev tunnel URL (used as the callback and WebSocket base URL) |
| `AcsConnectionString` | Connection string from your ACS resource (Azure Portal → Keys) |
| `VoiceLiveEndpoint` | Azure AI Services endpoint (the `.cognitiveservices.azure.com` URL, provisioned by `azd up`) |
| `FoundryAgentName` | Name of the Foundry Agent (default: `VoiceLiveAgent`, created by `azd up`) |
| `FoundryProjectName` | Name of the Foundry Project (default: `voiceAgentProject`, created by `azd up`) |
| `FoundryAgentVersion` | Agent version string (set automatically on App Service by `azd up`, copy locally from Azure Portal for dev) |
| `TransferPhoneNumber` | Phone number for call transfers (e.g. `+4922147114711`) |

> **Authentication**: This branch uses `DefaultAzureCredential`. For local development, sign in via `az login` or `azd auth login`. No API key needed — your Azure user must have the **Azure AI User** role on the AI Services resource (configured by `azd up`).

> **Tip**: You can also use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to keep credentials out of config files entirely.

### 4. Customize the system prompt

The agent's personality and behavior are defined in `Prompts/system-prompt.txt`. Edit this file directly — no JSON escaping needed, changes take effect on the next `dotnet run`.

### 5. Register the EventGrid webhook

In the Azure Portal, configure your ACS phone number to send **IncomingCall** events to your dev tunnel:

1. Go to your **ACS resource** → **Events**
2. Create an **Event Subscription**:
   - **Event Types**: Select `Incoming Call`
   - **Endpoint Type**: Web Hook
   - **Endpoint URL**: `https://<your-tunnel-url>/api/incomingCall`
3. Save — EventGrid will send a validation request, and the app will respond automatically

### 6. Run the app

```bash
cd ACSVoiceAgent
dotnet run
```

The app starts and listens for incoming calls. You should see:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 7. Test

1. Call your ACS phone number from any phone
2. The app answers the call and connects it to the AI voice agent
3. Try saying things like:
   - *"Can you look up my account? My customer ID is C-1001"*
   - *"What's the status of order ORD-1001?"*
   - *"I'd like to book an appointment for tomorrow at 2 PM"*
   - *"Transfer me to a human agent"*
   - *"Thanks, goodbye"* (triggers automatic call termination)

## Project Structure

```
├── Program.cs                          # Minimal API host, endpoints, WebSocket middleware
├── Helper.cs                           # EventGrid JSON parsing helpers
├── ACSVoiceAgent.csproj                # .NET 8 project file
├── appsettings.json                    # Configuration (defaults)
├── appsettings.Development.json        # Local development configuration (add your secrets here)
├── azure.yaml                          # azd project file with agent lifecycle hooks
├── Models/
│   └── CallSession.cs                  # Call session model (state per active call)
├── Prompts/
│   └── system-prompt.txt               # Agent system prompt (loaded into Foundry Agent)
├── Services/
│   ├── AcsMediaStreamingHandler.cs     # ACS WebSocket send/receive
│   ├── AgentFunctions.cs               # Client-side tool implementations + mock data
│   ├── AzureVoiceLiveService.cs        # Voice Live session via Foundry Agent, event loop, function dispatch
│   └── CallSessionManager.cs           # In-process call state (ConcurrentDictionary)
├── scripts/
│   ├── agent_manager.py                # Creates/deletes Foundry Agent + vector store (azd hooks)
│   ├── pyproject.toml                  # Python dependencies (azure-ai-projects, azure-identity)
│   └── docs/
│       └── product-catalog.md          # Product catalog indexed by file_search vector store
├── infra/
│   ├── main.bicep                      # Main infrastructure definition
│   ├── main.parameters.json            # Parameter mappings for azd
│   └── app/
│       ├── web.bicep                   # App Service + App Insights + Dashboard
│       ├── ai/
│       │   └── cognitive-services.bicep # AI Services + Foundry Project + model deployment
│       └── rbac/
│           └── openai-access.bicep     # Reusable RBAC role assignment module
└── README.md
```

## Call Termination

The `end_call` tool uses a non-blocking design to ensure goodbye audio plays fully:

1. The model generates goodbye audio + `end_call` function call in the same response
2. `end_call` sets a `_pendingHangUp` flag and returns immediately (does not block the event loop)
3. Audio deltas continue streaming to ACS while the event loop processes remaining events
4. When `ResponseAudioDone` fires, it triggers a delayed hang-up (2s buffer for ACS to finish playback)
5. `HangUpAsync(true)` disconnects the call; a 404 from ACS is handled gracefully (caller already hung up)

## Key Design Decisions

- **Foundry Agent as control plane**: Tool definitions and system prompt are managed in the Foundry Agent — not hardcoded in C#. This enables updating agent behavior (prompt, tools) without redeploying the app.
- **Hybrid tool dispatch**: Function tools are dispatched client-side in C# for low-latency execution. `file_search` runs server-side in Foundry for RAG over the product catalog. This gives the best of both worlds.
- **Entra ID authentication**: Uses `DefaultAzureCredential` instead of API keys. RBAC (`Azure AI User`) is provisioned via Bicep for the developer, App Service managed identity, and Foundry Project identity.
- **Stateless agent, ephemeral sessions**: The Foundry Agent itself is stateless. Conversation history lives only in the Voice Live session (server-side) and the in-process `CallSession` (C#). No cross-call persistence.
- **In-process state**: Active calls are tracked in a `ConcurrentDictionary`. If the process crashes, live calls are lost — this is intentional since WebSocket/audio sessions can't be resumed.
- **Audio format**: PCM 24kHz mono for ACS media streaming, PCM 16-bit for Voice Live.
- **HD voice**: Uses `en-US-Ava:DragonHDLatestNeural` via `AzureStandardVoice` for higher-quality speech synthesis.
- **Server-side audio processing**: Noise reduction (`FarField` mode) and echo cancellation are enabled in the Voice Live session to improve clarity on phone calls.
- **System prompt as file**: Stored in `Prompts/system-prompt.txt` and loaded into the Foundry Agent at creation time by `agent_manager.py`.
- **No durable orchestration**: Real-time bidirectional audio has zero recovery benefit from checkpointing. Post-call workflows (summarization, CRM updates) would be good candidates for durable orchestrations if added later.

## Differences from `main` Branch

| Aspect | `main` | `foundry-agent` |
|---|---|---|
| **Authentication** | API key | Entra ID (`DefaultAzureCredential`) |
| **Voice Live connection** | `VoiceLiveSessionOptions` with inline tools | `SessionTarget.FromAgent(AgentSessionConfig)` |
| **Tool definitions** | Hardcoded in `AzureVoiceLiveService.cs` | Defined in Foundry Agent (via `agent_manager.py`) |
| **System prompt** | Loaded from file into session options | Loaded from file into Foundry Agent metadata |
| **Product catalog** | N/A | `file_search` over vector store |
| **SDK version** | `Azure.AI.VoiceLive` 1.0.0 (GA) | `Azure.AI.VoiceLive` 1.1.0-beta.3 (preview) |
| **IaC** | App Service only | AI Services + Foundry Project + model + RBAC + App Service |
| **Agent lifecycle** | N/A | `azd` hooks create/delete agent + vector store |

## What Foundry Agent Adds over Pure Voice Live

The Foundry Agent doesn't change the realtime model or audio pipeline — it adds a management and evaluation layer on top:

| Capability | Pure Voice Live (`main`) | Foundry Agent (this branch) |
|---|---|---|
| **Agent versioning** | None — changes require redeployment | `create_version()` — compare v1 vs v2 side by side |
| **Tracing** | Manual logging only | Built-in Foundry tracing (tool calls, latencies, token usage) |
| **Evaluation SDK** | Build your own pipeline from scratch | Native integration — evaluators work out of the box |
| **Prompt optimization** | Manual trial-and-error | Foundry prompt optimizer iterates using eval scores |
| **file_search (RAG)** | N/A | Server-side retrieval over product catalog with measurable precision |
| **Dataset from traces** | Export and parse logs manually | Curate evaluation datasets directly from production traces |
| **A/B testing** | Deploy two separate apps | Two agent versions, same app, switch by version string |

**Cost impact**: Negligible. The realtime model (~95% of AI cost) is identical in both approaches. Foundry Agent metadata is free, vector store storage is ~$0.10/GB/day, and file_search retrieval only adds tokens on product queries.

## Scaling to Production (20K+ Calls/Day)

### Architecture Constraints

The app uses `ConcurrentDictionary<string, CallSession>` for in-process call state. This is intentional — live audio sessions can't survive a process restart — but it creates a constraint: **all events for a single call must reach the same app instance**.

A call involves three correlated requests:
1. `POST /api/incomingCall` — EventGrid delivers the incoming call event
2. `POST /api/callbacks/{id}` — ACS sends `CallConnected`, `CallDisconnected`, etc.
3. `WebSocket /ws` — ACS opens a bidirectional audio stream

If any of these land on a different instance than the others, the `ConcurrentDictionary` lookup fails and the call breaks (no session found, audio goes nowhere).

### Scaling Strategy: ARR Affinity + Scale Out

**ARR Affinity** (Application Request Routing) solves this. App Service sets a cookie on the first HTTP request, and ACS includes it on subsequent callbacks, routing them to the same instance. The WebSocket connection is inherently sticky once established.

| Requirement | Solution |
|---|---|
| Session routing | ARR Affinity (sticky sessions) on App Service |
| Compute | Scale up to P1v3 or P2v3 per instance |
| Horizontal scaling | 3-8 instances with auto-scaling |
| Health monitoring | Health check endpoint |

### Capacity Estimates

| SKU | Concurrent Calls per Instance | Instances for ~174 Peak Concurrent |
|---|---|---|
| B1 (1 core, 1.75GB) | ~50-80 | 3-4 |
| P1v3 (2 cores, 8GB) | ~100-150 | 2 |
| P2v3 (4 cores, 16GB) | ~200-300 | 1 |

> 20,000 calls/day × 5 min avg ÷ 1,440 min/day × 2.5 peak multiplier ≈ **174 peak concurrent calls** (assuming 12-hour peak window).

### What Does NOT Need to Change

- **`ConcurrentDictionary` pattern** — works correctly with sticky sessions; no need for Redis or distributed state
- **WebSocket handling** — inherently per-connection, already sticky
- **Voice Live session management** — each session is ephemeral and tied to one call

### Azure Service Quotas to Request

| Service | Default | Target | Action |
|---|---|---|---|
| ACS concurrent calls | 400 | 500+ | Support ticket |
| Voice Live concurrent sessions | Check region | 500+ | Support ticket |
| App Service instances | 10 | 10 | Default may suffice |

### Instance Failure: Impact & Mitigations

If an App Service instance goes down, all active calls on that instance are lost — the WebSocket, Voice Live session, and audio stream cannot be recovered. The blast radius depends on how many instances you run:

| Setup | Calls per Instance | Calls Lost per Failure |
|---|---|---|
| P2v3 × 1 | ~174 | **All calls** (single point of failure) |
| P1v3 × 2 | ~87 | ~87 (~50%) |
| B1 × 4 | ~44 | ~44 (~25%) |
| B1 × 6 | ~29 | ~29 (~17%) |

**Criticality: Medium.** App Service instances rarely crash outright. The main risks are deployment (planned), out-of-memory, unhandled exceptions, or platform maintenance. Affected callers hear silence or get disconnected and need to redial. Recovery is automatic — the instance restarts and new calls route normally within seconds.

**Mitigations (zero code changes):**

1. **More instances = smaller blast radius** — use 4-6 smaller instances instead of 1-2 large ones. ARR Affinity distributes calls evenly across them.
2. **Health checks** — configure `healthCheckPath` in App Service so unhealthy instances are detected and replaced before they crash.
3. **Deployment slots** — deploy to a staging slot, then swap. Existing WebSocket connections on the old slot stay alive until calls complete naturally. This eliminates deployment as a cause of dropped calls.
4. **Auto-heal** — App Service can automatically restart an instance when it detects memory pressure, slow responses, or high error rates.

**Why not Redis or distributed state?** The `WebSocket` and `VoiceLiveSession` objects are in-process handles that cannot be serialized or moved between machines. Any distributed store would only hold metadata — the live audio session is inherently tied to one process. Adding Redis would increase complexity and latency with zero failure recovery benefit.

### Alternative: Container Apps

Azure Container Apps can run many more granular replicas than App Service, reducing the blast radius of a single replica failure. App Service scales in whole VM instances (each B1 = 1 core/1.75GB), while Container Apps scales in lightweight containers (e.g., 0.5 vCPU / 1GB each) packed onto shared infrastructure.

| Platform | Replicas | Calls per Replica | Blast Radius per Failure |
|---|---|---|---|
| App Service (4× B1) | 4 | ~44 | ~44 calls lost (~25%) |
| Container Apps (10 replicas) | 10 | ~17 | ~17 calls lost (~10%) |
| Container Apps (20 replicas) | 20 | ~9 | ~9 calls lost (~5%) |

Additional advantages for this workload:

- **Finer auto-scaling** — scale from 3 to 20 replicas based on HTTP concurrent requests or custom metrics, more responsive than App Service auto-scale rules
- **Faster scale-out** — containers start in seconds vs. minutes for App Service instances
- **Per-second billing** — pay only for actual resource consumption, not idle whole VMs
- **Session affinity built-in** — `sticky-sessions` annotation, equivalent to ARR Affinity

**Trade-offs**: Requires a container image (Dockerfile or build pack), and networking/observability setup is slightly more involved than App Service. The application code requires **no changes** — only the hosting infrastructure differs.

## Deployment with Azure Developer CLI (azd)

The project includes full `azd` infrastructure to deploy to **Azure App Service** with a **Foundry Agent** created automatically as part of the provisioning flow.

### What gets provisioned

| Resource | SKU | Why |
|---|---|---|
| Resource Group | — | Container for all resources |
| AI Services | S0 | Hosts the Voice Live API + model deployments |
| Foundry Project | — | `voiceAgentProject` — manages the agent and vector store |
| Model Deployment | `gpt-4.1-mini` GlobalStandard | Realtime model for voice conversations |
| RBAC | Azure AI User | Grants access to developer, App Service identity, and project identity |
| App Service Plan | B1 (Linux) | Always On + WebSocket support required |
| App Service | .NET 8 on Linux | Hosts the voice agent app with SystemAssigned managed identity |
| Application Insights | — | Monitoring + live metrics |

### Post-provision hooks (automatic)

After infrastructure provisioning, `azd` runs `scripts/agent_manager.py create` which:
1. Uploads `scripts/docs/product-catalog.md` to Foundry Files
2. Creates a `ProductCatalog` vector store and indexes the file
3. Creates the `VoiceLiveAgent` (Standard Agent) with all 8 function tool definitions + `file_search`
4. Sets the agent version on the App Service app settings

### Deploy

1. **Install prerequisites**: [azd](https://aka.ms/azd-install), [az CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli), [Python 3.10+](https://www.python.org/), [uv](https://docs.astral.sh/uv/)

2. **Sign in**:

   ```bash
   azd auth login
   az login
   ```

3. **Initialize the environment**:

   ```bash
   azd init -e dev
   ```

4. **Set required variables**:

   ```bash
   azd env set ACS_CONNECTION_STRING "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>"
   azd env set TRANSFER_PHONE_NUMBER "+4922180102503"  # optional
   ```

5. **Provision and deploy**:

   ```bash
   azd up
   ```

   This will:
   - Create AI Services, Foundry Project, model deployment, and RBAC
   - Create the App Service with managed identity
   - Run `agent_manager.py create` to set up the Foundry Agent + vector store
   - Set `FoundryAgentVersion` on the App Service
   - Build and deploy the .NET app

5. **Update the ACS EventGrid webhook** to point to your new App Service URL:

   The `azd up` output prints `SERVICE_WEB_URI` (e.g. `https://app-xxxx.azurewebsites.net`). Use this to update your IncomingCall subscription:

   1. Go to the **Azure Portal** → your **ACS resource** → **Events**
   2. Click on your existing **Event Subscription** (or create one if this is a new ACS resource)
   3. Under **Endpoint**, change the URL to:
      ```
      https://<app-name>.azurewebsites.net/api/incomingCall
      ```
      Replace `<app-name>` with the actual App Service name from the `azd up` output.
   4. Click **Save**
   5. EventGrid will send a validation handshake — the deployed app handles this automatically

   > If you were previously using a dev tunnel URL, this replaces it. You can switch back to the dev tunnel for local development by updating the endpoint again.

### Subsequent deployments

After the initial `azd up`, use `azd deploy` to push code changes without re-provisioning infrastructure. To update the Foundry Agent (e.g., after changing the system prompt or product catalog), re-run `azd provision` — the `postprovision` hook recreates the agent.

### Teardown

```bash
azd down
```

This runs the `predown` hook which deletes the Foundry Agent, vector store, and uploaded file before tearing down infrastructure.

> **Note**: Do **not** use a Consumption plan — the app requires long-lived WebSocket connections and Always On to avoid cold starts mid-call.

## Conversation Transcription

The Voice Live SDK supports real-time transcription of both the agent's and caller's speech. This is **not enabled by default** — here are the options and trade-offs:

### Agent output transcript (free)

The model already generates text internally before synthesizing speech. Handling `SessionUpdateResponseAudioTranscriptDone` events exposes this text at no additional cost.

### Caller input transcript (additional cost)

Requires enabling `InputAudioTranscription` in session options, which runs a separate transcription model on the caller's audio. Available models:

| Model | Cost | Quality |
|---|---|---|
| `Whisper1` | Lowest | Good for most use cases |
| `Gpt4oMiniTranscribe` | Medium | Better accuracy |
| `Gpt4oTranscribe` | Higher | Best accuracy |
| `AzureSpeech` | Varies | Azure Speech Services pricing |

Transcription runs asynchronously and does **not** add latency to the audio path — the conversation sounds identical with or without it enabled.

### Alternatives to real-time transcription

- **Post-call batch transcription**: Buffer raw PCM audio during the call, then send to Azure Speech-to-Text batch API after disconnect. Cheaper than real-time, but not available until after the call ends.
- **Model-inferred summaries**: The model already understands what the caller said. A post-call summarization prompt can reconstruct the conversation without a separate transcription model — no extra cost, but less precise.

## Post-Call Evaluation with Foundry Evaluations SDK

The [Azure AI Foundry Evaluations SDK](https://learn.microsoft.com/en-us/azure/ai-studio/how-to/develop/evaluate-sdk) (`azure-ai-evaluation`) can evaluate agent quality on **transcripts** post-call. It works on text, not live audio — so the pattern is: capture transcripts during calls, then run batch evaluation.

### Data Available for Evaluation

| Data | Source in Code | Status |
|---|---|---|
| Tool invocations + args + results | `CallSession.ToolInvocations` | Already captured |
| Agent response text | `SessionUpdateResponseAudioTranscriptDone` | Available (free) |
| Caller speech text | `SessionUpdateConversationItemInputAudioTranscriptionCompleted` | Requires input transcription enabled |
| Call metadata (caller, duration, status) | `CallSession` | Already captured |

### Applicable Evaluators

| Evaluator | What It Measures | Applies To |
|---|---|---|
| `GroundednessEvaluator` | Are agent responses grounded in tool results? | Agent response + tool output |
| `RelevanceEvaluator` | Is the response relevant to what the caller asked? | Caller utterance + agent response |
| `CoherenceEvaluator` | Is the response logically consistent? | Agent response |
| `FluencyEvaluator` | Is the language natural? | Agent response |
| `ViolenceEvaluator` / `SelfHarmEvaluator` | Safety checks | Full conversation |
| Custom LLM-as-judge | Was the right tool called? Did the agent follow prompt rules? | Tool invocations + transcript |

### What Doesn't Apply

- **`SimilarityEvaluator`** — No ground-truth reference responses for live calls
- **Audio quality metrics** — SDK is text-only; use ACS call quality analytics instead

### Recommended Approach

1. **Enable input transcription** (`InputAudioTranscription` with `Gpt4oTranscribe` or `Whisper1`)
2. **Capture transcript pairs** in the `HandleSessionUpdateAsync` event loop — accumulate caller/agent utterances per call
3. **Store conversations** post-call (blob storage or database) as JSON
4. **Run batch evaluation** on stored transcripts using built-in + custom evaluators
5. **Custom evaluators** are most impactful for call centers: check intent detection accuracy, tool selection correctness, system prompt compliance, and resolution rate

> **Note**: SSML is not applicable in this architecture. The Voice Live API manages the text→TTS pipeline internally — there is no access to intermediate text for SSML wrapping. The DragonHD voice handles prosody (intonation, pacing, emphasis) automatically from context. Voice behavior is instead controlled through the **system prompt** (e.g., "speak slowly when reading order numbers", "use a warm tone for complaints").
