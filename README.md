# ACS Voice Agent with Azure AI Voice Live SDK

A C# ASP.NET Core minimal API that bridges **Azure Communication Services (ACS)** incoming phone calls with the **Azure AI Voice Live API** (`gpt-realtime-mini`) for real-time AI-powered voice conversations.

## Architecture

```
Phone Call → ACS (EventGrid) → /api/incomingCall
    → AnswerCall with bidirectional MediaStreaming
    → ACS Audio WebSocket (/ws) ↔ AcsMediaStreamingHandler ↔ AzureVoiceLiveService
    → VoiceLiveClient/VoiceLiveSession (Azure.AI.VoiceLive SDK)
    → gpt-realtime-mini with 8 function tools
```

## Agent Tools

| Tool | Description |
|---|---|
| `customer_lookup` | Look up customer info by phone number or customer ID |
| `order_status` | Check order tracking status |
| `check_appointment` | Check existing appointment details |
| `book_appointment` | Book a new appointment |
| `cancel_appointment` | Cancel an existing appointment |
| `search_knowledge_base` | Search FAQ/knowledge base by keyword |
| `transfer_call` | Transfer the call to a human agent via ACS (fixed number from config) |
| `end_call` | End the current call and disconnect |

Tools use simulated in-memory data (defined in `AgentFunctions.cs`) except `transfer_call` and `end_call`, which perform real ACS operations.

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
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment and resource management)
  - **communication** extension: `az extension add --name communication --yes`
- [Azure Dev Tunnels CLI](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started) (`devtunnel`) — for exposing your local server to ACS
- An **Azure Communication Services** resource with a phone number configured for incoming calls
- An **Azure AI Services** resource (kind: `AIServices`) with a `gpt-realtime-mini` model deployment
- (Optional) [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) (`azd`) — for automated Azure deployment

### Provisioning Azure Resources

Both the ACS resource and the AI Services resource are **long-lived, external resources** — they are not created by the Bicep templates in this repo. You need to set them up once.

#### Azure Communication Services

1. Create an ACS resource in the [Azure Portal](https://portal.azure.com) → **Create a resource** → search **Communication Services**
2. Choose a **Data Location** (e.g., `Europe`) and create
3. Go to **Phone Numbers** → **Get a number** — acquire a phone number with **Inbound calling** capability
4. Note the **Connection String** from **Settings → Keys** (you'll need this for configuration)

#### Azure AI Foundry + Model Deployment

The `gpt-realtime-mini` model is deployed through [Azure AI Foundry](https://ai.azure.com):

1. Go to [ai.azure.com](https://ai.azure.com) and create a **Foundry project** (or use an existing one)
   - The project must be in a region that supports `gpt-realtime-mini` (e.g., `swedencentral`)
2. In your project, go to **Model catalog** → search for `gpt-realtime-mini` → **Deploy**
   - Deployment name: `gpt-realtime-mini`
   - Deployment type: `Global Standard`
3. After deployment, get the connection details:
   - **Endpoint**: From the project's **Overview** page — the `.services.ai.azure.com` URL of the Foundry resource
   - **API Key**: From the AI Services resource in Azure Portal → **Resource Management → Keys and Endpoint**

> **Check model availability**: In AI Foundry, the model catalog shows which regions support each model. Alternatively: `az cognitiveservices model list --location <region> --query "[?model.name=='gpt-realtime-mini']"`

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

### 3. Configure local settings

All local settings (including secrets) go in `appsettings.Development.json`, which is **gitignored**. Copy the template and fill in your values:

```bash
cp appsettings.Development.template.json appsettings.Development.json
```

Then edit `appsettings.Development.json`:

```json
{
  "DevTunnelUri": "https://<your-tunnel-url>.devtunnels.ms",
  "AcsConnectionString": "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>",
  "VoiceLiveApiKey": "<your-api-key>",
  "VoiceLiveEndpoint": "https://<your-ai-services>.services.ai.azure.com",
  "VoiceLiveModel": "gpt-realtime-mini",
  "TransferPhoneNumber": "+1234567890"
}
```

| Setting | Description |
|---|---|
| `DevTunnelUri` | Your dev tunnel URL (callback and WebSocket base URL) |
| `AcsConnectionString` | Connection string from your ACS resource (Azure Portal → ACS resource → Settings → Keys) |
| `VoiceLiveApiKey` | API key from your Azure AI Services resource (Azure Portal → AI Services resource → Resource Management → Keys and Endpoint) |
| `VoiceLiveEndpoint` | Endpoint URL from the same Keys and Endpoint page (the `.services.ai.azure.com` URL) |
| `VoiceLiveModel` | Model deployment name (default: `gpt-realtime-mini`) |
| `TransferPhoneNumber` | Phone number for call transfers (E.164 format) |

> **Tip**: If you run `azd provision`, the postprovision hook automatically generates `appsettings.Development.json` with all values from the provisioned resources. You only need to fill in `DevTunnelUri` afterward.

> **Foundry Agent branch**: The `foundry-agent` branch uses a different template with additional settings (`FoundryAgentName`, `FoundryProjectName`) and no `VoiceLiveApiKey` (it uses Entra ID auth instead).

### 4. Customize the system prompt

The agent's personality and behavior are defined in `Prompts/system-prompt.txt`. Edit this file directly — no JSON escaping needed, changes take effect on the next `dotnet run`.

### 5. Register the EventGrid webhook

For local development, you need an EventGrid subscription on your ACS resource that points to your **dev tunnel URL**. This tells ACS to notify your local app when a phone call comes in.

1. Go to your **ACS resource** in the Azure Portal → **Events**
2. Create an **Event Subscription**:
   - **Name**: e.g. `local-dev` (you'll reuse/update this)
   - **Event Types**: Select `Incoming Call`
   - **Endpoint Type**: Web Hook
   - **Endpoint URL**: `https://<your-tunnel-url>/api/incomingCall`
3. Save — EventGrid will send a validation request to your tunnel, and the app will respond automatically

> **Note**: If your dev tunnel URL changes (e.g., you recreate the tunnel), you must update this EventGrid subscription to the new URL. You can also use a [persistent dev tunnel](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/get-started#create-a-tunnel) to keep a stable URL across sessions.

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

## Azure Deployment

Deploy to Azure using the [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/):

### Prerequisites

- An existing **ACS resource** with a phone number (ACS is not provisioned by Bicep — phone numbers require manual setup)
- An existing **Azure AI Services** resource with a `gpt-realtime-mini` model deployment

### First-time setup

```bash
azd init
azd env set ACS_CONNECTION_STRING "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>"
azd env set VOICE_LIVE_API_KEY "<your-ai-services-api-key>"
azd env set VOICE_LIVE_ENDPOINT "https://<your-ai-services>.services.ai.azure.com"
azd env set TRANSFER_PHONE_NUMBER "+1234567890"
azd env set VOICE_LIVE_MODEL "gpt-realtime-mini"
azd env set ACS_RESOURCE_NAME "<your-acs-resource-name>"
azd env set ACS_RESOURCE_GROUP "<your-acs-resource-group>"
```

### Deploy

```bash
azd up
```

This provisions (App Service, Log Analytics, App Insights, dashboard), deploys the app, and runs hooks that:
1. Generates `appsettings.Development.json` for local development (postprovision — first run only)
2. Creates/updates an **EventGrid subscription** on your ACS resource pointing to the App Service URL (postdeploy — every deploy)

### What Bicep provisions

| Resource | Provisioned by |
|---|---|
| Resource Group | Bicep |
| App Service + Plan (B1, Linux, Always On) | Bicep |
| Log Analytics + Application Insights | Bicep |
| Portal Dashboard | Bicep |
| **ACS** (with phone numbers) | **External** — pre-existing |
| **AI Services** (with model deployment) | **External** — pre-existing |
| **EventGrid subscription** | Postdeploy hook (via `az rest` — runs on every deploy) |

## Project Structure

```
├── Program.cs                          # Minimal API host, endpoints, WebSocket middleware
├── Helper.cs                           # EventGrid JSON parsing helpers
├── ACSVoiceAgent.csproj                # .NET 8 project file
├── appsettings.json                    # Configuration (defaults, no secrets)
├── appsettings.Development.json        # Local dev settings (gitignored — secrets + dev config)
├── appsettings.Development.template.json # Template for appsettings.Development.json
├── Models/
│   └── CallSession.cs                  # Call session model (state per active call)
├── Prompts/
│   └── system-prompt.txt               # Agent system prompt (plain text, easy to edit)
├── Services/
│   ├── AcsMediaStreamingHandler.cs     # ACS WebSocket send/receive
│   ├── AgentFunctions.cs               # Simulated tool implementations + mock data
│   ├── AzureVoiceLiveService.cs        # Voice Live session, tool definitions, event loop
│   └── CallSessionManager.cs           # In-process call state (ConcurrentDictionary)
├── infra/                              # Bicep IaC templates
├── azure.yaml                          # Azure Developer CLI project file
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

- **In-process state**: Active calls are tracked in a `ConcurrentDictionary`. If the process crashes, live calls are lost — this is intentional since WebSocket/audio sessions can't be resumed.
- **Audio format**: PCM 24kHz mono for ACS media streaming, PCM 16-bit for Voice Live.
- **HD voice**: Uses `en-US-Ava:DragonHDLatestNeural` via `AzureStandardVoice` for higher-quality speech synthesis.
- **Server-side audio processing**: Noise reduction (`FarField` mode) and echo cancellation are enabled in the Voice Live session to improve clarity on phone calls.
- **System prompt as file**: Stored in `Prompts/system-prompt.txt` (copied to output on build) instead of in appsettings, for readability and easier editing.
- **No durable orchestration**: Real-time bidirectional audio has zero recovery benefit from checkpointing. Post-call workflows (summarization, CRM updates) would be good candidates for durable orchestrations if added later.

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

## Foundry Agent Branch

The [`foundry-agent`](https://github.com/gbelenky/ACSVoiceAgent/tree/foundry-agent) branch replaces the direct Voice Live SDK integration with an **Azure AI Foundry Agent** that manages the conversation. Key differences:

| | `main` (this branch) | `foundry-agent` |
|---|---|---|
| **AI integration** | Direct `VoiceLiveClient` → `gpt-realtime-mini` | Foundry Agent → Voice Live API |
| **Tool definitions** | Inline `VoiceLiveFunctionDefinition` in code | Defined on the Foundry Agent (managed in AI Foundry portal) |
| **Authentication** | API key (`AzureKeyCredential`) | Entra ID (`DefaultAzureCredential`) |
| **SDK version** | `Azure.AI.VoiceLive` 1.0.0 | `Azure.AI.VoiceLive` 1.1.0-beta.3 |
| **Hold audio** | None | 440Hz ring-back tone during Voice Live session connect |
| **Configuration** | `VoiceLiveApiKey` + `VoiceLiveEndpoint` | `FoundryAgentName` + `FoundryProjectName` + project endpoint |

The Foundry Agent approach allows managing tools, instructions, and knowledge sources through the AI Foundry portal without code changes. See the [`foundry-agent` branch README](https://github.com/gbelenky/ACSVoiceAgent/tree/foundry-agent) for setup instructions.

### Working with the Foundry Agent branch

The two branches have **different NuGet packages**, **different `appsettings.Development.json` structures**, and **separate azd environments**. The recommended approach is to clone into a separate folder:

```bash
# Clone into a separate folder for the foundry-agent branch
git clone -b foundry-agent https://github.com/gbelenky/ACSVoiceAgent.git ACSVoiceAgent-foundry
cd ACSVoiceAgent-foundry

# Create appsettings.Development.json from the branch's template
cp appsettings.Development.template.json appsettings.Development.json
# Fill in your values — different settings than main (see the template)
#   Key differences: no VoiceLiveApiKey, uses FoundryAgentName + FoundryProjectName

# Restore packages and build
dotnet restore
dotnet build

# If using azd, create a separate environment for this branch
azd env new voiceagent-foundry
# Then set the branch-specific env vars (see foundry-agent README)
```

This avoids having to clean build artifacts, recreate config, and restore packages every time you switch. It also lets you run both versions side-by-side for comparison.

> **Important**: Do not copy `appsettings.Development.json` between the two folders — the settings are incompatible. Always start from each branch's own template.
