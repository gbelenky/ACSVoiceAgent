# Building a Real-Time AI Voice Agent with Azure (Foundry Agent)

**One-Day Training Lab — Step by Step**

> **Branch**: `foundry-agent` — This training uses **Microsoft Foundry Agents** to manage tools, instructions, and voice configuration in the cloud. The agent is created and versioned via the AgentManager CLI tool. The C# application only handles the audio bridge and client-side tool dispatch.

---

## Training Overview

| | |
|---|---|
| **Duration** | Full day (~7 hours with breaks) |
| **Level** | Intermediate — assumes Azure Portal familiarity and basic C# knowledge |
| **What you'll build** | A production-grade AI voice agent backed by a Foundry Agent that answers phone calls, holds natural conversations, and executes business logic in real time |
| **What you'll learn** | Generative AI fundamentals, Microsoft Foundry Agents, Voice Live SDK with agent mode, ACS telephony, function calling, file search (RAG), and cloud deployment with `azd` |

### Agenda

| Time | Lab | Topic |
|---|---|---|
| 09:00–09:45 | 1 | Concepts — GenAI, LLMs, and the Realtime API |
| 09:45–10:30 | 2 | Microsoft Foundry — Create resources and deploy a Realtime model |
| 10:30–10:45 | | *Break* |
| 10:45–11:30 | 3 | Azure Communication Services — Phone numbers and Direct Routing |
| 11:30–12:15 | 4 | Project walkthrough — Understand the voice agent codebase |
| 12:15–13:00 | | *Lunch* |
| 13:00–13:45 | 5 | Local development — Dev tunnels, configuration, and first call |
| 13:45–14:30 | 6 | Voice Live SDK deep dive — Sessions, audio, and VAD tuning |
| 14:30–14:45 | | *Break* |
| 14:45–15:30 | 7 | Function calling — Add a new tool to the voice agent |
| 15:30–16:15 | 8 | Deploy to Azure with `azd` |
| 16:15–16:45 | 9 | Production considerations — Scaling, monitoring, and content safety |
| 16:45–17:00 | | Wrap-up and Q&A |

### Detailed Lab Contents

**Lab 1: Concepts — GenAI, LLMs, and the Realtime API**
- 1.1 What is Generative AI?
- 1.2 Large Language Models (LLMs)
- 1.3 Tokens, Context Windows, and Temperature
- 1.4 The Chat Completions API
- 1.5 Why a Realtime Audio API?
- 1.6 The Voice Live API
- 1.7 Key Takeaways

**Lab 2: Microsoft Foundry — Create a Project, Deploy Models, and Set Up Authentication**
- 2.1 Create a Foundry Project and Resource
- 2.2 Deploy the Chat Model (gpt-4.1-mini)
- 2.3 Set Up Entra ID Authentication (RBAC)
- 2.4 Understanding Content Safety Filters
- 2.5 Checkpoint

**Lab 3: Azure Communication Services — Phone Numbers and Direct Routing**
- 3.1 Create an ACS Resource
- 3.2 Get a Phone Number
- 3.3 Register the EventGrid Webhook
- 3.4 How EventGrid Connects to Your App
- 3.5 Key Takeaways

**Lab 4: Project Walkthrough — Understand the Voice Agent Codebase**
- 4.1 Clone the Repository
- 4.2 Solution Structure
- 4.3 Program.cs — The Application Host
- 4.4 AcsMediaStreamingHandler — ACS WebSocket Bridge
- 4.5 AzureVoiceLiveService — Voice Live with Foundry Agent Mode
- 4.6 AgentManager — Creating and Versioning the Foundry Agent
- 4.7 AgentFunctions — Tool Implementations (Client-Side)
- 4.8 CallSession and CallSessionManager — State Management
- 4.9 Key Takeaways

**Lab 5: Local Development — Dev Tunnels, Configuration, and First Call**
- 5.1 Create and Start a Dev Tunnel
- 5.2 Create the Foundry Agent (AgentManager)
- 5.3 Configure appsettings.Development.json
- 5.4 Register the EventGrid Webhook
- 5.5 Build and Run
- 5.6 Make Your First Call
- 5.7 Key Takeaways

**Lab 6: Voice Live SDK Deep Dive — Sessions, Audio, and VAD Tuning**
- 6.1 The VoiceLiveClient Lifecycle
- 6.2 VoiceLiveSessionOptions Reference
- 6.3 The Audio Bridge in Detail
- 6.4 Turn Detection and Barge-In *(Experiment: VAD Tuning)*
- 6.5 Noise Reduction and Echo Cancellation
- 6.6 The Event Loop
- 6.7 Exercise: Change the Voice

**Lab 7: Function Calling — Add a New Tool to the Voice Agent**
- 7.1 How Function Calling Works with Foundry Agents
- 7.2 Exercise: Add a `check_weather` Tool
- 7.3 The Three-File Pattern (AgentManager + AgentFunctions + Dispatch)
- 7.4 Real vs. Simulated Tools — The Transfer Call Example
- 7.5 Key Takeaways

**Lab 8: Deploy to Azure with `azd`**
- 8.1 What is the Azure Developer CLI?
- 8.2 The azure.yaml File and Hooks
- 8.3 The Bicep Infrastructure (App Service + AI Services + Foundry Project)
- 8.4 Authenticate with Azure
- 8.5 Initialize the Project
- 8.6 Provision and Deploy
- 8.7 Verify the Deployment
- 8.8 Update the EventGrid Webhook
- 8.9 Key Takeaways

**Lab 9: Production Considerations — Scaling, Monitoring, and Content Safety**
- 9.1 Hosting Requirements for Voice Agents
- 9.2 Scaling and High Availability
- 9.3 Monitoring and Observability
- 9.4 Content Safety and Responsible AI
- 9.5 Post-Call Processing
- 9.6 Security Checklist
- 9.7 Cost Estimation
- 9.8 Key Takeaways

> **Hands-on activities**: Exercise in 6.7, Experiment in 6.4, Exercise in 7.2

### Prerequisites

Before the training day, each participant needs:

- [ ] An **Azure subscription** with Owner or Contributor access
- [ ] [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- [ ] [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (`az`) installed
- [ ] [Azure Developer CLI](https://aka.ms/azd-install) (`azd`) installed
- [ ] [Git](https://git-scm.com/) installed
- [ ] A code editor (Visual Studio Code recommended, with the C# Dev Kit extension)
- [ ] A phone (mobile or desk) to make test calls
- [ ] Azure Dev Tunnels CLI: `winget install Microsoft.devtunnel` (Windows) or `brew install devtunnel` (macOS)

---

## Lab 1: Concepts — GenAI, LLMs, and the Realtime API

**Duration**: 45 minutes  
**Objective**: Understand the foundational concepts before writing any code.

### 1.1 What is Generative AI?

Generative AI (GenAI) refers to AI models that create new content — text, images, audio, code — rather than simply classifying or predicting. The models that power GenAI are called **Large Language Models (LLMs)**.

**Key concepts:**

| Term | Definition |
|---|---|
| **LLM** | A neural network trained on massive text data that can understand and generate human language. Examples: GPT-4o, GPT-4o-mini |
| **Token** | The basic unit LLMs process. Roughly ~4 characters of English text. Both input and output are measured in tokens |
| **Prompt** | The instruction you give the model. Quality of the prompt directly affects quality of the output |
| **System prompt** | A special instruction that sets the model's personality, rules, and constraints. The user never sees it |
| **Context window** | Maximum number of tokens the model can process at once (input + output). GPT-4o context: 128K tokens |
| **Inference** | The process of the model generating a response. Each inference call has a cost based on tokens consumed |

### 1.2 Traditional Text LLMs vs. Realtime Audio Models

Traditional LLMs work with text: you send text in, you get text back. To build a voice application with a traditional LLM, you'd need a pipeline:

```
Caller speaks --> Speech-to-Text --> LLM (text) --> Text-to-Speech --> Caller hears
```

This pipeline has **three separate model calls** with cumulative latency (typically 500ms–1500ms total). Each component is configured, billed, and can fail independently.

**Realtime audio models** collapse this into one:

```
Caller speaks --> Realtime Model (audio in, audio out) --> Caller hears
```

The model natively understands audio input and generates audio output. No intermediate text conversion is required. Latency drops to ~200ms.

### 1.3 Microsoft Foundry and the GPT Realtime API

**Microsoft Foundry** (formerly Azure AI Foundry / Azure AI Studio / Azure OpenAI Service) is the Microsoft platform for deploying and managing AI models. It provides:

- Model catalog with GPT-5 series, GPT-4.1 series, GPT Realtime, and other models
- API endpoints with built-in content safety filters
- Usage monitoring and quota management
- API keys and Azure Active Directory authentication

The **GPT Realtime API** is the family of models that process audio directly:

| Model | Strengths | Audio Cost (per 1M tokens) |
|---|---|---|
| `gpt-realtime-mini` | Low latency, cost-effective, good for most scenarios | Input: $10 / Output: $20 |
| `gpt-realtime` | Higher reasoning quality, better at complex tasks | Input: $32 / Output: $64 |
| `gpt-realtime-1.5` | Latest generation, improved quality and features | Input: $32 / Output: $64 |

> **For this training, we use `gpt-4.1-mini`** as the Foundry Agent's reasoning model, with Voice Live handling the realtime audio processing behind the scenes.

### 1.4 What is the Azure AI Voice Live Service?

The **Voice Live Service** is an Azure layer that sits between your application and the GPT Realtime model. It adds telephony-grade audio features that the raw Realtime API doesn't provide:

| Feature | Raw Realtime API | Voice Live Service |
|---|---|---|
| Audio I/O | Yes | Yes |
| Function calling | Yes | Yes |
| Azure HD voices (500+ voices) | No (6 OpenAI voices only) | Yes |
| Server-side noise reduction | No | Yes (NearField, FarField, DeepNoise) |
| Echo cancellation | No | Yes |
| Semantic VAD (turn detection) | Basic | Yes (Azure Semantic VAD) |
| Content safety filters | Yes (basic) | Yes (full Azure AI content filtering) |

The Voice Live SDK (`Azure.AI.VoiceLive` NuGet package) provides a **strongly-typed C# client** — no raw WebSocket management required.

### 1.5 What is Azure Communication Services (ACS)?

**Azure Communication Services** connects your application to the telephone network (PSTN). For this solution, ACS provides:

- **Phone numbers** — inbound calling capable numbers
- **Call Automation** — programmatic control (answer, transfer, hang up)
- **Media Streaming** — bidirectional audio via WebSocket (PCM 24kHz mono)
- **EventGrid integration** — event-driven notifications for incoming calls
- **Direct Routing** — connect your own SBC infrastructure (optional, for enterprise)

### 1.6 Function Calling (Tool Use)

A critical capability of the Realtime API is **function calling**. You define functions (tools) that the model can invoke during a conversation. The model decides *when* to call a function based on the conversation context.

```
Caller: "Can you check my order status? Order 001."

Model thinks: The user wants order status -> I should call order_status(order_id="001")

Model calls: order_status({"order_id": "001"})

Your code returns: {"orderId":"001","status":"Shipped","estimatedDelivery":"2026-03-27",
                    "items":"Wireless Headphones x1","trackingNumber":"1Z999AA10123456784"}

Model speaks: "Your order 001 has been shipped and should arrive by March 27th.
              The tracking number is 1Z999AA10123456784."
```

The model **never sees your database or APIs directly** — it only sees the function definitions (name, description, parameters) and the results you return. You control what data the model can access.

### 1.7 Architecture Overview

Here's the complete architecture of what we're building:

```
+----------+     +-----------------+      +------------------------------+      +----------------------+
|          |     |                 |      |       App Service            |      |   Microsoft Foundry  |
|  Caller  |---->|  ACS            |----> |                              |----> |                      |
|  (Phone) |<----|  (PSTN Gateway) |<---- |  ASP.NET Core Minimal API    |<---- |  Voice Live Service  |
|          |     |                 |      |                              |      |  Foundry Agent       |
+----------+     +-----------------+      |  +---------------------+     |      |                      |
                  SIP/RTP                 |  |  AcsMediaStreaming   |    |      |  +----------------+  |
                  <------------>          |  |  Handler             |    |      |  | Noise Reduction|  |
                                          |  |  (WebSocket bridge)  |    |      |  | Echo Cancel    |  |
                  EventGrid               |  +---------------------+     |      |  | VAD            |  |
                  ------------->          |  +---------------------+     |      |  | HD Voice       |  |
                                          |  |  AzureVoiceLive     |     |      |  +----------------+  |
                  HTTP Callbacks          |  |  Service            |     |      |                      |
                  ------------->          |  |  (session + tools)  |     |      |  +----------------+  |
                                          |  +---------------------+     |      |  | Function       |  |
                                          |  +---------------------+     |      |  | Calling        |  |
                                          |  |  AgentFunctions      |    |      |  | (8 tools)      |  |
                                          |  |  (business logic)    |    |      |  +----------------+  |
                                          |  +---------------------+     |      |                      |
                                          +------------------------------+      +----------------------+
```

**Data flow summary:**

1. Caller dials a phone number -> ACS receives the call
2. ACS fires an EventGrid event -> App Service answers the call
3. ACS opens a bidirectional WebSocket -> App Service bridges audio to Voice Live
4. Voice Live processes speech, generates responses, and triggers function calls
5. App Service executes functions and sends results back to Voice Live
6. Voice Live speaks the answer -> audio flows back through ACS -> caller hears it

### Lab 1 Review Questions

1. What is the difference between a traditional LLM pipeline (STT -> LLM -> TTS) and the Realtime API?
2. What does Voice Live add on top of the raw Realtime API?
3. Why do we need ACS in this architecture — can't the Realtime API handle phone calls directly?
4. What is function calling, and who decides when a function should be invoked?

---

## Lab 2: Microsoft Foundry — Create a Project, Deploy Models, and Set Up Authentication

**Duration**: 45 minutes  
**Objective**: Create a Microsoft Foundry project, deploy the chat model used by the Foundry Agent, and configure Entra ID authentication.

> **Key difference from the main branch**: The Foundry Agent approach uses a cloud-managed agent that holds the tools, instructions, and voice configuration. Authentication uses Entra ID (no API keys). The agent uses `gpt-4.1-mini` as its reasoning model, while Voice Live handles the realtime audio processing behind the scenes.

### 2.1 Create a Foundry Project and Resource

The simplest way to get started is through the Foundry portal, which creates the underlying Azure resource automatically.

1. Go to [Microsoft Foundry](https://ai.azure.com) and sign in
2. Make sure the **New Foundry** toggle (top of the page) is **on**
3. In the upper-left corner, click the project name → **Create new project**
4. Give your project a name (e.g., `voiceagent-project`) and click **Create project**

   Under **Advanced options** you can customize:

   | Field | Value |
   |---|---|
   | **Resource group** | Create new: `rg-voiceagent-training` |
   | **Location** | `West Europe` (must support Realtime and GPT-4.1 models) |

   If you leave defaults, a new resource group and **Foundry resource** are created automatically.

> **Important**: The location must support both GPT Realtime and GPT-4.1 models. Check the [model availability table](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#model-summary-table-and-region-availability) if you're unsure. Common regions: `West Europe`, `East US 2`, `Sweden Central`.

> **Need custom Azure config?** If your organization requires specific naming, security controls, or cost tags, you can also create the resource via the [Azure Portal](https://portal.azure.com) or via [Bicep templates](https://learn.microsoft.com/en-us/azure/foundry/how-to/create-resource-template).

After creation, note the **Project endpoint** from the project settings page — it has the format:
```
https://<foundry-resource-name>.services.ai.azure.com/api/projects/<project-name>
```

> **Write this down** — you'll need it in Lab 5.

### 2.2 Deploy the Chat Model (gpt-4.1-mini)

The Foundry Agent uses `gpt-4.1-mini` as its reasoning model. Voice Live handles the audio-to-text and text-to-audio conversion using the realtime model behind the scenes.

1. In the Foundry portal, select **Discover** (upper-right navigation) → **Models** (left pane)
2. Search for `gpt-4.1-mini` and select it
3. Click **Deploy** → **Custom settings**
4. Configure:

   | Field | Value |
   |---|---|
   | **Deployment name** | `gpt-4.1-mini` |
   | **Deployment type** | Global Standard |

5. Click **Deploy**

> **Why gpt-4.1-mini and not gpt-realtime-mini?** In the Foundry Agent approach, Voice Live manages the realtime audio session. The Foundry Agent definition specifies `gpt-4.1-mini` as the reasoning model, which handles tool selection and response generation. Voice Live bridges between the realtime audio stream and the agent's reasoning model.

### 2.3 Set Up Entra ID Authentication (RBAC)

Unlike the main branch (which uses API keys), the Foundry Agent requires **Entra ID authentication** (`DefaultAzureCredential`). You need the **Cognitive Services User** role on the Foundry resource.

1. Go to Azure Portal → your **Foundry resource** → **Access control (IAM)**
2. Click **+ Add** → **Add role assignment**
3. Select role: **Cognitive Services User**
4. Assign to: **User** → select your Azure account
5. Click **Review + assign**

For local development, `DefaultAzureCredential` will use your `az login` session. For production, the App Service's Managed Identity will be used (configured automatically by Bicep in Lab 8).

> **Verify**: Run `az account show` to confirm you're logged in with the correct account.

### 2.4 Understanding Content Safety Filters

Your Foundry resource comes with **built-in content safety filters** that are active by default:

| Category | Default Threshold | What it filters |
|---|---|---|
| Hate & Fairness | Medium | Slurs, stereotypes, discriminatory content |
| Sexual | Medium | Explicit sexual content |
| Violence | Medium | Graphic violence, weapons instructions |
| Self-Harm | Medium | Self-harm instructions or encouragement |
| Jailbreak | On | Prompt injection attempts |
| Protected Material | On | Copyrighted text reproduction |

These filters work at the platform level — no code changes required.

### Lab 2 Checkpoint

- [ ] Foundry project and resource created
- [ ] Project endpoint noted
- [ ] `gpt-4.1-mini` model deployed
- [ ] Cognitive Services User role assigned to your account
- [ ] `az login` verified

---

## Lab 3: Azure Communication Services — Phone Numbers and Telephony

**Duration**: 45 minutes  
**Objective**: Create an ACS resource, acquire a phone number, and understand how calls reach your application.

### 3.1 Create an ACS Resource

1. In the Azure Portal, click **Create a resource** → search for **Communication Services**
2. Click **Create** and fill in:

   | Field | Value |
   |---|---|
   | **Subscription** | Your Azure subscription |
   | **Resource group** | `rg-voiceagent-training` (same as Lab 2) |
   | **Name** | `acs-voiceagent-<your-initials>` |
   | **Data location** | United States (or Europe, depending on compliance needs) |

3. Review and **Create**

### 3.2 Get a Phone Number

1. Go to your ACS resource → **Phone numbers** (under Telephony)
2. Click **+ Get** 
3. Configure:

   | Field | Value |
   |---|---|
   | **Country** | United Kingdom (recommended) |
   | **Number type** | Geographic (e.g. London +44 20) or Toll-Free (0800) |
   | **Calling** | Make and receive calls |
   | **SMS** | None needed (optional) |

4. Select an available number and confirm purchase

> **Recommended: UK numbers** — United Kingdom geographic and toll-free numbers are widely available in ACS, support both inbound and outbound calling, and work well for training purposes. If UK numbers are not available in your region, try United States toll-free numbers as a fallback.

### 3.3 Get the ACS Connection String

1. Go to your ACS resource → **Keys** (under Settings)
2. Copy **Connection string** (Primary)

   Format: `endpoint=https://<name>.communication.azure.com/;accesskey=<base64-key>`

> **Write this down** — you'll need it in Lab 5.

### 3.4 How Calls Reach Your Application — The EventGrid Pattern

When someone calls your ACS phone number, here's what happens:

```
1. Caller dials +1-800-XXX-XXXX
        |
2. PSTN routes to ACS
        |
3. ACS receives the call but does NOT answer it yet
        |
4. ACS publishes an "IncomingCall" event to Azure EventGrid
        |
5. EventGrid delivers the event via HTTP POST to your webhook
   (POST /api/incomingCall)
        |
6. Your app decides what to do:
   -> AnswerCallAsync() -- answer the call with media streaming
   -> RejectCallAsync() -- reject the call
   -> (do nothing -- call times out after 30 seconds)
        |
7. ACS answers the call and opens:
   - HTTP callbacks (CallConnected, CallDisconnected, etc.)
   - WebSocket for bidirectional audio streaming
```

**Why EventGrid instead of direct webhook?** EventGrid provides at-least-once delivery, retry logic, dead-letter queues, and subscription filtering — all without custom infrastructure.

> **Advanced: Event Grid Filtering**
>
> A single ACS resource can have **multiple Event Grid subscriptions** for `IncomingCall` events, each pointing to a different webhook. However, only one handler can answer each call — the `incomingCallContext` token is single-use.
>
> To route different phone numbers to different applications, use **advanced filters** on the subscription:
>
> | Field | Operator | Value |
> |---|---|---|
> | `data.to.phoneNumber.value` | String contains | `+441234567890` |
>
> This way, calls to one number go to App A and calls to another number go to App B — no conflicts, no race conditions. You configure this under **Event Subscription → Filters → Advanced Filters** in the portal.
>
> For a single-app scenario (like this training), a single subscription with no filters is sufficient.

### 3.5 Set Up the IncomingCall Event Subscription

We'll complete this configuration in Lab 5 after our dev tunnel is ready. For now, understand the setup:

1. ACS resource → **Events** → **+ Event Subscription**
2. Event type: `Microsoft.Communication.IncomingCall`
3. Endpoint: `https://<your-app-url>/api/incomingCall`
4. EventGrid sends a validation request to verify your endpoint

### Lab 3 Checkpoint

- [ ] ACS resource created
- [ ] Phone number acquired and noted
- [ ] Connection string copied
- [ ] Understand the EventGrid → webhook call flow

---

## Lab 4: Project Walkthrough — Understand the Voice Agent Codebase

**Duration**: 45 minutes  
**Objective**: Clone the repo and understand every component before running it.

### 4.1 Clone the Repository

```bash
git clone -b foundry-agent https://github.com/gbelenky/ACSVoiceAgent.git ACSVoiceAgent-foundry
cd ACSVoiceAgent-foundry
dotnet restore
```

### 4.2 Project Structure

```
ACSVoiceAgent-foundry/
|-- Program.cs                          # Entry point -- endpoints, DI, WebSocket middleware
|-- Helper.cs                           # EventGrid JSON parsing utilities
|-- Models/
|   +-- CallSession.cs                  # Per-call state model (includes transcript tracking)
|-- Prompts/
|   +-- system-prompt.txt               # Agent personality and rules (loaded by AgentManager)
|-- Services/
|   |-- AcsMediaStreamingHandler.cs     # ACS WebSocket send/receive
|   |-- AgentFunctions.cs               # Business logic tools (simulated data, client-side)
|   |-- AzureVoiceLiveService.cs        # Voice Live session with Foundry Agent mode
|   +-- CallSessionManager.cs           # In-process session tracking
|-- scripts/
|   +-- AgentManager/                   # CLI tool to create/delete the Foundry Agent
|       |-- Program.cs                  # Agent lifecycle management
|       +-- AgentManager.csproj
|-- docs/
|   +-- product-catalog.md             # Product catalog document (uploaded to vector store)
|-- infra/                              # Bicep IaC (App Service + AI Services + Foundry Project)
|-- appsettings.json                    # Configuration (defaults)
|-- appsettings.Development.template.json # Template for local dev config
|-- appsettings.Development.json        # Local dev config (gitignored)
|-- azure.yaml                          # Azure Developer CLI project definition + hooks
+-- ACSVoiceAgent.csproj                # .NET 8 project file
```

> **Key differences from the main branch**: The `scripts/AgentManager` directory contains a CLI tool that creates the Foundry Agent with all tool definitions and a vector store for product catalog search. The `docs/` directory holds documents uploaded to the vector store. Tool definitions live in AgentManager, not in the Voice Live service code.

### 4.3 NuGet Packages

Open `ACSVoiceAgent.csproj` and review the dependencies:

| Package | Version | Purpose |
|---|---|---|
| `Azure.AI.VoiceLive` | 1.1.0-beta.3 | Voice Live SDK — supports Foundry Agent mode (`SessionTarget.FromAgent`) |
| `Azure.Communication.CallAutomation` | 1.5.1 | ACS Call Automation — answer, transfer, hang up calls programmatically |
| `Azure.Messaging.EventGrid` | 5.0.0 | Parse EventGrid events (IncomingCall triggers) |
| `Azure.Identity` | 1.19.0 | Entra ID authentication (`DefaultAzureCredential`) — **required** for Foundry Agent mode |
| `Microsoft.ApplicationInsights.AspNetCore` | 2.22.0 | Monitoring and telemetry |

> **Note**: The Foundry Agent branch uses `Azure.AI.VoiceLive` 1.1.0-beta.3 (not 1.0.0 like the main branch). This version adds `SessionTarget.FromAgent` and `AgentSessionConfig` for connecting to a Foundry-managed agent.

### 4.4 Program.cs — Endpoints and Wiring

Open `Program.cs`. This file is the entire API surface. Let's walk through each section:

**Service Registration (lines 1–25):**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

var client = new CallAutomationClient(acsConnectionString);
builder.Services.AddSingleton(client);
builder.Services.AddSingleton<CallSessionManager>();
```
- `CallAutomationClient` is the ACS SDK client — singleton because it's thread-safe
- `CallSessionManager` tracks all active calls in memory

**Incoming Call Endpoint (POST /api/incomingCall):**

This is the EventGrid webhook. When ACS receives a phone call:

1. Validates the EventGrid subscription (one-time handshake)
2. Extracts the caller's phone number and call context
3. Calls `client.AnswerCallAsync()` with:
   - A **callback URI** (`/api/callbacks/{id}`) for ACS events
   - A **WebSocket URI** (`/ws`) for bidirectional audio
   - **MediaStreamingOptions**: PCM 24kHz mono, bidirectional, start immediately

**Callback Endpoint (POST /api/callbacks/{contextId}):**

ACS sends lifecycle events here:

| Event | Action |
|---|---|
| `CallConnected` | Create a `CallSession` via `CallSessionManager` |
| `CallDisconnected` | Remove and dispose the session |
| `CallTransferAccepted` | Log success |
| `CallTransferFailed` | Log error |

**WebSocket Middleware (/ws):**

When ACS starts media streaming, it connects to `/ws`:

1. Accept the WebSocket connection
2. Create `AcsMediaStreamingHandler` (wraps the WebSocket)
3. Create `AzureVoiceLiveService` (bridges to Microsoft Foundry)
4. Wait for `CallConnected` session (via `CallSessionManager.WaitForSessionAsync()`)
5. Start the Voice Live session
6. **Audio loop**: receive ACS audio → base64 decode → send to Voice Live

### 4.5 CallSessionManager — Session Lifecycle

Open `Services/CallSessionManager.cs`.

**The problem it solves**: The `/api/callbacks` endpoint (where `CallConnected` arrives) and the `/ws` endpoint (where the WebSocket connects) are **separate HTTP requests**. They need to share the same `CallSession`.

**The solution**: A `Channel<CallSession>` — a thread-safe, async-friendly queue:

```
/api/callbacks -> CallConnected -> CreateSession() -> Channel.Write(session)
                                                          |
                                                          v
/ws -> HandleWebSocket() -> WaitForSessionAsync() -> Channel.Read() -> binds session
```

This gives zero-latency, non-blocking handoff without polling.

### 4.6 AzureVoiceLiveService — Voice Live with Foundry Agent Mode

Open `Services/AzureVoiceLiveService.cs`. This is the most important file — but it's **simpler** than the main branch because the Foundry Agent handles tools, instructions, and voice config.

**CreateSessionAsync()** — Sets up the Voice Live connection using a Foundry Agent:

1. Reads config: `VoiceLiveEndpoint`, `FoundryAgentName`, `FoundryProjectName`, `FoundryAgentVersion`
2. Creates `AgentSessionConfig` with agent name and project name
3. Authenticates via `DefaultAzureCredential` (Entra ID — no API key)
4. Connects using **`SessionTarget.FromAgent(agentConfig)`** — the agent defines tools, instructions, and voice
5. Configures audio format only (PCM 16-bit for ACS compatibility)
6. Starts background event loop: `Task.Run(ReceiveEventsAsync)`

```csharp
// Build agent session config
var agentConfig = new AgentSessionConfig(agentName, projectName);
if (!string.IsNullOrEmpty(agentVersion))
    agentConfig.AgentVersion = agentVersion;

// Agent mode requires Entra ID authentication (no API key)
var client = new VoiceLiveClient(new Uri(endpoint), new DefaultAzureCredential());

// Connect using SessionTarget.FromAgent
_session = await client.StartSessionAsync(
    SessionTarget.FromAgent(agentConfig), cancellationToken);

// Only configure audio format — everything else is in the agent
var options = new VoiceLiveSessionOptions
{
    InputAudioFormat = InputAudioFormat.Pcm16,
    OutputAudioFormat = OutputAudioFormat.Pcm16,
};
await _session.ConfigureSessionAsync(options, cancellationToken);
```

> **Key difference**: In the main branch, `VoiceLiveSessionOptions` includes model, instructions, voice, VAD, noise reduction, and all tool definitions. In the Foundry Agent branch, only audio format is configured in code — everything else lives in the agent definition managed by AgentManager.

**Hold Audio** — While Voice Live connects (6–14s warm start, longer on cold start), the app plays a ring-back tone (440Hz) to the caller so they don't hear silence:

```csharp
_ = PlayHoldAudioAsync(_holdAudioCts.Token);
```

The hold audio stops automatically when the Voice Live session is ready (`SessionUpdateSessionUpdated` event).

**ReceiveEventsAsync()** — Processes server events (same pattern as main branch):

| Event | Action |
|---|---|
| `SessionUpdated` | Stop hold audio, send proactive greeting |
| `AudioDelta` | Forward audio bytes to ACS |
| `SpeechStarted` | Send `StopAudio` to ACS (barge-in) |
| `FunctionCallArgumentsDone` | Execute client-side tool and send result back |
| `AudioTranscriptDone` | Log agent speech + add to transcript |
| `InputAudioTranscriptionCompleted` | Log caller speech + add to transcript |

**HandleFunctionCallAsync()** — Tool dispatch (same pattern as main branch):

```csharp
result = functionName switch
{
    "customer_lookup" => AgentFunctions.LookupCustomer(args),
    "order_status" => AgentFunctions.CheckOrderStatus(args),
    // ... other tools
    "transfer_call" => await TransferCallAsync(args),
    "end_call" => await EndCallAsync(args),
    _ => // error
};

await _session.AddItemAsync(new FunctionCallOutputItem(callId, result));
await _session.StartResponseAsync();
```

> **Note on file_search**: The Foundry Agent also has a `file_search` tool (for the product catalog) but this is handled **server-side** by the Foundry platform — your code never sees file_search invocations. Only client-side function tools appear in `HandleFunctionCallAsync()`.

### 4.7 AgentManager — Creating and Versioning the Foundry Agent

Open `scripts/AgentManager/Program.cs`. This CLI tool manages the Foundry Agent lifecycle.

**`create` command:**
1. Uploads `docs/product-catalog.md` to the Foundry project file store
2. Creates a vector store and adds the product catalog
3. Defines all 8 function tools (`ResponseTool.CreateFunctionTool`)
4. Adds `file_search` tool pointing to the vector store
5. Creates the agent with `CreateAgentVersionAsync()`
6. Outputs the agent ID (name:version format)

**`delete` command:**
Deletes the agent version, vector store, and uploaded file.

**Running AgentManager locally:**
```bash
# Set the project endpoint
export PROJECT_ENDPOINT="https://<ai-services>.services.ai.azure.com/api/projects/<project>"

# Create the agent
dotnet run --project scripts/AgentManager -- create
```

> **This step is required before running the app** — without a Foundry Agent, the Voice Live session cannot start. You'll do this in Lab 5.

### 4.8 AgentFunctions — Simulated Business Logic (Client-Side)

Open `Services/AgentFunctions.cs`.

All tools use **in-memory mock data** (static dictionaries). In a real application, these would call your CRM, ERP, database, or APIs.

| Function | Data Source |
|---|---|
| `LookupCustomer()` | Static dictionary of 2 customers |
| `CheckOrderStatus()` | Static dictionary of 3 orders |
| `CheckAppointment()` | Static dictionary of 2 appointments |
| `BookAppointment()` | Returns simulated confirmation |
| `CancelAppointment()` | Returns simulated cancellation |
| `SearchKnowledgeBase()` | 7 FAQ entries with keyword matching |

**Test data you can use during calls:**

*Customers:*

| Identifier | Name | Phone | Tier | Balance |
|---|---|---|---|---|
| `12345` | Genady Belenky | +14255551234 | Gold | $150.00 |
| `67890` | John Smith | +14255555678 | Silver | $45.50 |

Lookup works by customer ID (`12345`) or phone number (`+14255551234`).

*Orders:*

| Order ID | Status | Items | Estimated Delivery | Tracking |
|---|---|---|---|---|
| `001` | Shipped | Wireless Headphones x1 | 2026-03-27 | 1Z999AA10123456784 |
| `002` | Processing | USB-C Hub x2 | 2026-03-30 | (none yet) |
| `003` | Delivered | Laptop Stand x1 | 2026-03-20 | 1Z999AA10987654321 |

*Appointments (by customer ID):*

| Customer ID | Appointment ID | Date | Time | Type |
|---|---|---|---|---|
| `12345` | APT-100 | 2026-03-28 | 10:00 | Service Review |
| `67890` | APT-101 | 2026-03-29 | 14:30 | Account Setup |

*FAQ keywords:* `return`, `refund`, `shipping`, `delivery`, `hours`, `warranty`, `cancel`, `subscription`, `payment`, `contact`, `support`

> **Key point**: The model never sees this code or the raw data. It only sees the function *definitions* (name, description, parameter schema) and the *results* your code returns.

### 4.9 AcsMediaStreamingHandler — WebSocket Wrapper

Open `Services/AcsMediaStreamingHandler.cs`.

Simple wrapper over the ACS WebSocket:

- `SendMessageAsync()` — Sends JSON to ACS (outbound audio or stop command)
- `ReceiveMessageAsync()` — Reads JSON from ACS (inbound caller audio)
- `OutStreamingData.GetAudioDataForOutbound()` — Wraps PCM bytes in ACS JSON format
- `OutStreamingData.GetStopAudioForOutbound()` — Sends stop command (for barge-in)

### 4.10 System Prompt

Open `Prompts/system-prompt.txt`:

```
You are a Customer Care assistant called Ava.

When you greet the caller, briefly introduce yourself and let them know you can help with:
- Looking up account information
- Checking order status
- Checking, booking or canceling appointments
- Searching the knowledge base
- Transferring to a human agent
- Ending the call

Your first question is about the caller's customer ID. Use the customer name during the conversation.
Be conversational, friendly, and concise in your responses.

RULES:
- NEVER speak the name of any tool or function out loud.
- When the caller wants to end the conversation, say a VERY brief goodbye and then invoke end_call.
- Always use the appropriate tool when the caller requests an action.
```

**Key observations:**
- Plain text — no JSON escaping needed
- In the Foundry branch, the system prompt is loaded by **AgentManager** when creating the agent version, not by the app at runtime. Editing requires re-running AgentManager to create a new agent version.
- Sets the agent's personality (friendly, concise)
- Establishes rules (never say tool names, use tools proactively)

### Lab 4 Review Questions

1. How does `SessionTarget.FromAgent` differ from passing `VoiceLiveSessionOptions` directly?
2. What is the role of AgentManager, and when do you need to run it?
3. Which tool invocations are handled client-side vs. server-side?
4. Why does the app play hold audio, and when does it stop?

---

## Lab 5: Local Development — Dev Tunnels, Configuration, and First Call

**Duration**: 45 minutes  
**Objective**: Create the Foundry Agent, configure the app, start it locally, and make your first AI voice call.

### 5.1 Create a Dev Tunnel

Your local app runs on `localhost:5000`, but ACS needs a public URL of your app to send events and connect WebSockets. A **dev tunnel** creates a secure public URL that forwards traffic to your local machine.

```bash
# Login to Azure (if not already logged in)
az login

# Login to the dev tunnel service (required before creating tunnels)
devtunnel user login

# Create a tunnel with anonymous access (required for ACS/EventGrid)
devtunnel create --allow-anonymous

# Map port 5000
devtunnel port create -p 5000

# Start the tunnel
devtunnel host
```

You'll see output like:
```
Connect via browser: https://abc123xyz.devtunnels.ms
Inspect network activity: https://abc123xyz.devtunnels.ms:4040
```

**Copy the tunnel URL** (e.g., `https://abc123xyz.devtunnels.ms`). Keep this terminal running.

> **Note**: The tunnel must stay open for the entire duration of local development. Open a new terminal for the remaining steps.

### 5.2 Create the Foundry Agent (AgentManager)

Before running the app, you need to create the Foundry Agent that defines the tools, instructions, and voice configuration.

```bash
# Set the project endpoint (from Lab 2.2)
# Windows (PowerShell):
$env:PROJECT_ENDPOINT = "https://<ai-services>.services.ai.azure.com/api/projects/<project-name>"

# Linux/macOS:
export PROJECT_ENDPOINT="https://<ai-services>.services.ai.azure.com/api/projects/<project-name>"

# Create the agent
dotnet run --project scripts/AgentManager -- create
```

You should see output like:
```
Creating Voice Live Standard Agent...
  Uploading product catalog document...
  FILE_ID: file-abc123
  Creating vector store...
  VECTORSTORE_ID: vs-xyz789
  Waiting for vector store processing...
  Vector store ready!
  Creating Standard Agent...
  AGENT_ID: VoiceLiveAgent:1
==================================================
Voice Live Standard Agent created successfully!
  Agent ID:        VoiceLiveAgent:1
  Vector Store ID: vs-xyz789
  File ID:         file-abc123
  Function tools:  8 (client-side)
  file_search:     product catalog (server-side)
==================================================
```

**Note the Agent ID** — the version number after the colon (e.g., `1`) is needed for the config.

> **Important**: This step requires your `az login` session to have the **Cognitive Services User** role on the AI Services resource (Lab 2.4).

### 5.3 Configure Application Settings

Copy the template to create your local settings file (this file is in `.gitignore` and won't be committed):

```bash
cp appsettings.Development.template.json appsettings.Development.json
```

Then fill in your values:

```json
{
  "DevTunnelUri": "https://<your-tunnel-url>.devtunnels.ms",
  "AcsConnectionString": "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>",
  "VoiceLiveEndpoint": "https://<ai-services>.services.ai.azure.com/api/projects/<project-name>",
  "FoundryAgentName": "VoiceLiveAgent",
  "FoundryProjectName": "<your-project-name>",
  "FoundryAgentVersion": "1",
  "TransferPhoneNumber": "+441234567890"
}
```

Fill in the values from Labs 2, 3, and 5.2:

| Setting | Source |
|---|---|
| `DevTunnelUri` | Your dev tunnel URL from step 5.1 |
| `AcsConnectionString` | ACS resource → Keys (Lab 3) |
| `VoiceLiveEndpoint` | Foundry Project endpoint (Lab 2.2) — format: `https://<name>.services.ai.azure.com/api/projects/<project>` |
| `FoundryAgentName` | `VoiceLiveAgent` (the agent name from AgentManager) |
| `FoundryProjectName` | Your Foundry project name (Lab 2.2) |
| `FoundryAgentVersion` | Version from AgentManager output (e.g., `1`) |
| `TransferPhoneNumber` | Phone number for call transfers (E.164 format) |

> **No API key needed** — authentication uses `DefaultAzureCredential`, which picks up your `az login` session locally and Managed Identity in production.

### 5.4 Register the EventGrid Webhook

Now that you have a public URL, register it with ACS:

1. Go to Azure Portal → your **ACS resource** → **Events**
2. Click **+ Event Subscription**
3. Fill in:

   | Field | Value |
   |---|---|
   | **Name** | `incoming-call-webhook` |
   | **Event Schema** | Event Grid Schema |
   | **Filter to Event Types** | Check only `Incoming Call` |
   | **Endpoint Type** | Web Hook |
   | **Endpoint** | `https://<your-tunnel>.devtunnels.ms/api/incomingCall` |

4. Click **Create**

> **Tip: Multiple apps sharing one ACS resource?** If your ACS resource handles calls for multiple phone numbers and you want to route them to different apps, add an **Advanced Filter** instead of creating a second subscription:
>
> **Filters tab → Advanced Filters → + Add new filter**
> - Key: `data.to.phoneNumber.value`
> - Operator: `String contains`
> - Value: your phone number (e.g. `+441234567890`)
>
> This ensures only calls to *your* number trigger *your* webhook.

EventGrid will send a validation request to your endpoint. The app handles this automatically — but the app needs to be running first. So let's start it.

### 5.5 Build and Run

```bash
cd ACSVoiceAgent-foundry
dotnet build
dotnet run
```

You should see:
```
info: Program[0]      App base URL resolved to: https://abc123xyz.devtunnels.ms
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

> If you created the EventGrid subscription before starting the app, go back to the portal and click **Resend** on the subscription to trigger validation again.

### 5.6 Make Your First Call

1. **Pick up your phone** and dial the ACS phone number you acquired in Lab 3
2. Watch the terminal logs — you should see:

   ```
   info: Program[0] Incoming Call event received
   info: Program[0] Incoming call from +441234567890
   info: Program[0] Answered call. Connection ID: ...
   info: Program[0] Call connected. ConnectionId: ...
   info: Program[0] ACS WebSocket connected
   info: AzureVoiceLiveService[0] Playing hold audio while connecting to Voice Live
   info: AzureVoiceLiveService[0] Connecting to Voice Live at ... with Foundry Agent 'VoiceLiveAgent'...
   info: AzureVoiceLiveService[0] Connected to Voice Live with Foundry Agent successfully
   info: AzureVoiceLiveService[0] Stopping hold audio
   info: AzureVoiceLiveService[0] Sending proactive greeting request
   ```

3. **Listen** — you'll first hear a brief ring-back tone (hold audio) while Voice Live connects, then the agent greets you: "Hi, I'm Ava, your customer care assistant..."

4. **Try these conversations** (use the exact test data from `AgentFunctions.cs`):

   > "My customer ID is 12345"
   > 
   > (Agent calls `customer_lookup` and greets you: "Hello Genady!")

   > "What's the status of order 001?"
   > 
   > (Agent calls `order_status` -- "Your order has been shipped, estimated delivery March 27th")

   > "What about order 002?"
   > 
   > (Agent calls `order_status` -- "That order is still processing, estimated delivery March 30th")

   > "Do I have any appointments?"
   > 
   > (Agent calls `check_appointment` -- "You have a Service Review on March 28th at 10:00 AM")

   > "I'd like to book an appointment for 2026-04-15 at 2 PM"
   >
   > (Agent calls `book_appointment` and confirms with a new APT-xxx ID)

   > "What's your return policy?"
   > 
   > (Agent calls `search_knowledge_base` -- "30-day return policy for unused items...")

   > "Thanks, goodbye"
   >
   > (Agent says goodbye and hangs up)

   > **Tip**: Try customer ID `67890` (John Smith) or ask about order `003` (Delivered) for different responses. Any unrecognized ID returns a "not found" error that the agent will relay politely.

### 5.7 Understanding the Logs

While on the call, watch the terminal output. Key log lines to look for:

```
# Voice Live connected and agent starts speaking
info: AzureVoiceLiveService[0] Voice Live session created
info: AzureVoiceLiveService[0] Voice Live session updated and ready
info: AzureVoiceLiveService[0] Agent transcript: Hi! I'm Ava...

# Caller speaks and a tool is invoked
info: AzureVoiceLiveService[0] Speech started (barge-in)
info: AzureVoiceLiveService[0] Function call: customer_lookup with call_id: call_xxx, args: {"identifier":"12345"}
info: AzureVoiceLiveService[0] Function customer_lookup completed with result: {"name":"Genady Belenky",...}

# Caller asks about products (handled server-side by file_search — no log in your app)

# Call ends
warn: AzureVoiceLiveService[0] End call requested for ...
warn: AzureVoiceLiveService[0] Waiting 2s before hanging up call ...
warn: AzureVoiceLiveService[0] HangUpAsync completed successfully
info: Program[0] Call disconnected. CorrelationId: ...
```

### 5.8 Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No logs when calling | EventGrid webhook not registered or wrong URL | Re-check the endpoint URL includes `/api/incomingCall` |
| "Incoming Call event received" but no answer | ACS connection string wrong | Verify `AcsConnectionString` in config |
| Call answers but only ring-back tone, no greeting | Voice Live or Foundry Agent failed | Check `VoiceLiveEndpoint`, `FoundryAgentName`, and `FoundryAgentVersion` |
| `AuthenticationFailedError` in logs | Entra ID not configured | Run `az login` and verify Cognitive Services User role (Lab 2.4) |
| "No call session found after 30s" | Race condition (rare) | Retry the call |
| Tunnel errors | Dev tunnel not running | Restart `devtunnel host` and update the config URL |

### Lab 5 Checkpoint

- [ ] Dev tunnel running with public URL
- [ ] Foundry Agent created via AgentManager (agent ID noted)
- [ ] `appsettings.Development.json` configured with all values
- [ ] EventGrid subscription registered
- [ ] App runs and answers calls
- [ ] Successfully had a conversation with the AI agent

---

## Lab 6: Voice Live SDK Deep Dive — Sessions, Audio, and VAD Tuning

**Duration**: 45 minutes  
**Objective**: Understand the Voice Live SDK internals and tune audio parameters.

### 6.1 VoiceLiveClient and VoiceLiveSession

The Voice Live SDK has two main classes:

```csharp
// Client: connects to Microsoft Foundry (Entra ID auth for agent mode)
var client = new VoiceLiveClient(new Uri(endpoint), new DefaultAzureCredential());

// Session: represents one active conversation, connected to a Foundry Agent
var agentConfig = new AgentSessionConfig(agentName, projectName);
var session = await client.StartSessionAsync(
    SessionTarget.FromAgent(agentConfig), cancellationToken);
```

The **client** is the connection factory. The **session** is the active conversation — it holds the audio stream, the conversation history, and the tool state. One session per phone call.

> **Agent mode vs. direct mode**: In the main branch, `client.StartSessionAsync(sessionOptions)` passes all configuration (model, tools, voice, VAD) inline. In this branch, `SessionTarget.FromAgent(agentConfig)` tells Voice Live to use a Foundry Agent that was created externally via AgentManager. Only audio format settings are configured client-side via `session.ConfigureSessionAsync(options)`.

### 6.2 Session Options in Agent Mode

`VoiceLiveSessionOptions` is the central configuration object for a Voice Live session. In **agent mode**, most properties (Model, Instructions, Voice, Tools, VAD) are defined in the Foundry Agent — you only configure audio format client-side via `session.ConfigureSessionAsync()`. The full reference below is still valuable for understanding what the agent controls.

> **In this branch**: Only `InputAudioFormat` and `OutputAudioFormat` are set in code. Everything else is configured in the Foundry Agent definition.

#### Session Identity

| Property | Type | Description |
|---|---|---|
| `Model` | `string` | The deployment name of the realtime model to use. Must match the deployment name in your Microsoft Foundry resource (e.g., `gpt-realtime-mini`). This is **not** the catalog model name — it's the name you chose when deploying. |
| `Instructions` | `string` | The system prompt that guides the model's behavior for the entire session. Defines personality, rules, constraints, and response style. Loaded from `Prompts/system-prompt.txt` in our app. Can be up to tens of thousands of characters. |
| `Temperature` | `float?` | Controls randomness in the model's output. Range: `0.0` (deterministic) to `1.0` (most creative). Default: `0.7`. For a customer service agent, lower values (0.6-0.7) keep responses consistent. Higher values make the agent more varied but less predictable. |

#### Modalities

| Property | Type | Description |
|---|---|---|
| `Modalities` | `IList<InteractionModality>` | Which input/output modes the session supports. For voice agents, set both `Text` and `Audio`. The model will accept audio input and produce both audio output (spoken to the caller) and text output (transcripts for logging). If you only set `Text`, the model won't generate audio — useful for testing without audio. |

In our app, we explicitly set both modalities:

```csharp
sessionOptions.Modalities.Clear();
sessionOptions.Modalities.Add(InteractionModality.Text);
sessionOptions.Modalities.Add(InteractionModality.Audio);
```

#### Audio Format

| Property | Type | Description |
|---|---|---|
| `InputAudioFormat` | `InputAudioFormat?` | Format of audio sent to the model. Options: `Pcm16` (16-bit PCM), `G711Alaw`, `G711Ulaw`. Default: `Pcm16`. ACS media streaming sends PCM16 at 24kHz, so `Pcm16` is the correct choice. G.711 formats are for direct telephony integration without ACS. |
| `OutputAudioFormat` | `OutputAudioFormat?` | Format of audio returned by the model. Options: `Pcm16`, `G711Alaw`, `G711Ulaw`. Default: `Pcm16`. Must match what ACS expects for playback — `Pcm16` in our case. |
| `InputAudioSamplingRate` | `int?` | Input audio sampling rate in Hz. For `Pcm16`: valid values are `8000`, `16000`, `24000`. For `G711Alaw`/`G711Ulaw`: only `8000`. When omitted, the service auto-detects from the audio stream. ACS sends 24kHz PCM, so you can leave this unset or explicitly set `24000`. |

#### Voice Configuration

| Property | Type | Description |
|---|---|---|
| `Voice` | `VoiceProvider` | Which voice the model uses to speak. This is a base class with multiple implementations (see Section 6.3 below). Our app uses `AzureStandardVoice` which gives access to 500+ Azure HD neural voices. |

#### Audio Processing (Telephony Features)

These properties are **unique to Voice Live** — they are not available in the raw Realtime API:

| Property | Type | Description |
|---|---|---|
| `InputAudioNoiseReduction` | `AudioNoiseReduction` | Server-side noise reduction applied to the caller's audio before the model processes it. Constructed with a mode: `FarField` (phone/speakerphone — our default), `NearField` (headset), or `AzureDeepNoiseSuppression` (very noisy environments). Without this, the model may mishear callers in noisy conditions. |
| `InputAudioEchoCancellation` | `AudioEchoCancellation` | Removes echo caused by the agent's own voice being picked up by the caller's microphone. Essential for speakerphone calls. In our app, we use the default constructor `new AudioEchoCancellation()` which enables standard echo cancellation. |

#### Turn Detection (VAD)

| Property | Type | Description |
|---|---|---|
| `TurnDetection` | `BinaryData` (set via typed classes) | Controls how the model detects when the caller has finished speaking so it can begin responding. This property accepts different VAD (Voice Activity Detection) strategies. |

Two turn detection strategies are available:

**`ServerVadTurnDetection`** (used in our app) — Classic server-side VAD:

| Sub-Property | Type | Description |
|---|---|---|
| `Threshold` | `float` | Speech detection sensitivity. Range: `0.0` to `1.0`. Higher = requires louder/clearer speech to trigger. Default: `0.5`. Raise to `0.7` for noisy environments. |
| `PrefixPadding` | `TimeSpan` | How much audio before the speech detection point to include. `300ms` captures the beginning of words that might otherwise be clipped. |
| `SilenceDuration` | `TimeSpan` | How long the caller must be silent before the model considers the turn complete and starts responding. `500ms` is a good balance. Lower (300ms) = faster but may interrupt; higher (800ms) = more patient but feels slower. |

**`AzureSemanticVadTurnDetection`** — AI-powered turn detection (recommended by Microsoft):

Instead of relying purely on silence duration, this uses a language model to understand whether the caller has finished their thought. This avoids the common problem where a caller pauses mid-sentence (thinking) and the agent jumps in prematurely.

Variants: `AzureSemanticVadTurnDetectionEn` (English-optimized), `AzureSemanticVadTurnDetectionMultilingual` (multilingual).

```csharp
// Alternative: semantic VAD (understands meaning, not just silence)
TurnDetection = new AzureSemanticVadTurnDetection()
```

> **Tip**: Start with `ServerVadTurnDetection` (simpler, predictable). Switch to `AzureSemanticVadTurnDetection` if callers complain about being interrupted mid-sentence.

#### Input Audio Transcription

| Property | Type | Description |
|---|---|---|
| `InputAudioTranscription` | `AudioInputTranscriptionOptions` | When set, the service transcribes the caller's speech and includes it in the session events. Useful for logging what the caller said. Our app currently does not enable this — we only log the agent's transcript via `SessionUpdateResponseAudioTranscriptDone`. |

#### Output Timestamps

| Property | Type | Description |
|---|---|---|
| `OutputAudioTimestampTypes` | `IList<AudioTimestampType>` | Types of timestamps to include in the audio response. Useful for synchronizing audio playback with visual elements (e.g., captions, avatar lip-sync). Not used in our telephony scenario but relevant for video/avatar applications. |

#### Tools (Function Calling)

| Property | Type | Description |
|---|---|---|
| `Tools` | `IList<VoiceLiveToolDefinition>` | Definitions of functions the model can call during the conversation. Each tool is a `VoiceLiveFunctionDefinition` with a name, description, and JSON Schema for parameters. The model decides when to call a tool based on the conversation context. See Lab 7 for a deep dive. |
| `ToolChoice` | `ToolChoiceOption` | Controls when the model uses tools. Options: `Auto` (model decides — default), `Required` (must use a tool), `None` (no tools). For most voice agents, leave as `Auto`. |

#### Response Length

| Property | Type | Description |
|---|---|---|
| `MaxResponseOutputTokens` | `MaxResponseOutputTokensOption` | Limits how many tokens the model generates per response. Useful to prevent very long answers on a phone call. When unset, the model uses its default limit. For phone agents, shorter responses are better — consider setting this to prevent monologues. |

#### Avatar and Animation

| Property | Type | Description |
|---|---|---|
| `Animation` | `AnimationOptions` | Animation configuration for visual applications. Not used in telephony. |
| `Avatar` | `AvatarConfiguration` | Configuration for avatar streaming (e.g., a visual avatar that lip-syncs with the agent's speech). Not used in our phone-only scenario but available for video call or kiosk applications. |

#### How Our App Configures the Session (Agent Mode)

In the Foundry Agent branch, session configuration is minimal — the agent handles everything else:

```csharp
// Build agent session config
var agentConfig = new AgentSessionConfig(agentName, projectName);
agentConfig.AgentVersion = agentVersion;

// Agent mode uses Entra ID (DefaultAzureCredential), not API key
var client = new VoiceLiveClient(new Uri(endpoint), new DefaultAzureCredential());

// Connect to the Foundry Agent — tools, instructions, voice, VAD are all in the agent
_session = await client.StartSessionAsync(
    SessionTarget.FromAgent(agentConfig), cancellationToken);

// Only configure audio format (must match ACS PCM 24kHz mono)
var options = new VoiceLiveSessionOptions
{
    InputAudioFormat = InputAudioFormat.Pcm16,
    OutputAudioFormat = OutputAudioFormat.Pcm16,
};
await _session.ConfigureSessionAsync(options, cancellationToken);
```

Compare this with the **main branch** (direct mode), where the full `VoiceLiveSessionOptions` includes model, instructions, voice, VAD, noise reduction, tools, and modalities — all configured in code.

> **Key takeaway**: In agent mode, the Foundry Agent definition is the "source of truth" for the agent's behavior. Code only handles audio format and the event loop. To change the voice, tools, or instructions, update the agent definition via AgentManager and create a new version.

### 6.3 Voice Options

In the Foundry Agent branch, the voice is configured in the agent definition (managed by AgentManager via the Foundry portal or API). Voice Live supports **all Azure TTS voices**, not just the 6 built-in OpenAI voices:

| Category | Example | SDK Class |
|---|---|---|
| Azure Standard/HD | `en-US-Ava:DragonHDLatestNeural` | `AzureStandardVoice` |
| Azure Custom Neural | Your custom-trained voice | `AzureCustomVoice` |

The `DragonHD` voices are the latest generation — more natural, lower latency for real-time use.

Browse the full voice catalog: [Azure TTS Voice Gallery](https://speech.microsoft.com/portal/voicegallery)

### 6.4 Voice Activity Detection (VAD) Tuning

VAD controls how the model decides when the caller has stopped speaking. This is critical for conversational quality.

> **Note**: In the Foundry Agent branch, VAD settings are part of the agent definition. To experiment with the settings described below, you would need to update the agent definition in the Foundry portal or modify AgentManager. The concepts are the same as in the main branch.

**Reference**: These are the VAD settings you can configure in the agent or via `VoiceLiveSessionOptions` (in direct mode):

**Make the agent wait longer before responding (less interrupting):**
```csharp
TurnDetection = new ServerVadTurnDetection
{
    Threshold = 0.5f,
    PrefixPadding = TimeSpan.FromMilliseconds(300),
    SilenceDuration = TimeSpan.FromMilliseconds(800)   // Was 500ms — now waits longer
}
```

**Make the agent respond faster (more responsive, but may interrupt):**
```csharp
TurnDetection = new ServerVadTurnDetection
{
    Threshold = 0.5f,
    PrefixPadding = TimeSpan.FromMilliseconds(300),
    SilenceDuration = TimeSpan.FromMilliseconds(300)   // Was 500ms — responds sooner
}
```

**Handle noisy environments (raise threshold):**
```csharp
TurnDetection = new ServerVadTurnDetection
{
    Threshold = 0.7f,                                   // Was 0.5 — requires louder speech
    PrefixPadding = TimeSpan.FromMilliseconds(300),
    SilenceDuration = TimeSpan.FromMilliseconds(500)
}
```

> **Important**: In the Foundry Agent branch, these VAD settings are defined in the agent, not in code. To experiment, you can switch to the **main branch** where these are set directly in `AzureVoiceLiveService.cs`, or modify the agent definition via the Foundry portal. Changes to the agent definition require creating a new agent version via AgentManager.

**Try Azure Semantic VAD (AI-powered turn detection):**

Instead of silence-based detection, semantic VAD uses a language model to understand whether the caller has finished their thought. This helps when callers pause mid-sentence to think:

```csharp
// Replace the ServerVadTurnDetection block with:
TurnDetection = new AzureSemanticVadTurnDetection()
```

Semantic VAD is especially useful for complex conversations where callers give long, multi-part answers (e.g., describing a problem). The trade-off is slightly higher latency since it processes meaning, not just silence.

### 6.5 Noise Reduction Modes

Voice Live offers three server-side noise reduction modes:

| Mode | Use Case | How it works |
|---|---|---|
| `FarField` | Phone calls, speakerphone | Optimized for audio where the mic is far from the speaker. **Best for PSTN calls.** |
| `NearField` | Headset, handset to ear | Less aggressive — assumes clean near-field audio |
| `AzureDeepNoiseSuppression` | Very noisy environments (factory, street) | Most aggressive AI-based suppression. May slightly affect voice naturalness |

The current app uses `FarField` — optimal for phone calls where callers may be on speakerphone or in moderate noise.

### 6.6 Barge-In — How Interruption Works

When the agent is speaking and the caller starts talking:

1. Voice Live detects `SpeechStarted` (caller is speaking)
2. Our app receives the `SessionUpdateInputAudioBufferSpeechStarted` event
3. We send `StopAudio` to ACS → ACS immediately stops playing the agent's audio
4. The model processes the caller's interruption and generates a new response

This is handled in `HandleSessionUpdateAsync()`:

```csharp
case SessionUpdateInputAudioBufferSpeechStarted:
    _logger.LogInformation("Speech started (barge-in)");
    await _mediaStreaming.SendMessageAsync(OutStreamingData.GetStopAudioForOutbound());
    break;
```

Without barge-in, callers would have to wait for the agent to finish speaking before they could respond — unacceptable for a phone experience.

### 6.7 Exercise: Change the Voice

In the Foundry Agent branch, the voice is configured in the agent definition. To change it, you would update the agent via the Foundry portal or modify AgentManager. For reference, these are popular Azure HD voices:

| Voice | Regional Accent |
|---|---|
| `en-US-Ava:DragonHDLatestNeural` | American (current default) |
| `en-US-Andrew:DragonHDLatestNeural` | American (male) |
| `en-GB-Libby:DragonHDLatestNeural` | British |
| `en-AU-Natasha:DragonHDLatestNeural` | Australian |

> **Tip**: To quickly experiment with voices, try the [main branch](https://github.com/gbelenky/ACSVoiceAgent) where the voice is set directly in code via `AzureStandardVoice`.

### Lab 6 Checkpoint

- [ ] Understand VoiceLiveClient vs. VoiceLiveSession
- [ ] Understand agent mode vs. direct mode session configuration
- [ ] Understand barge-in mechanism
- [ ] Know how to change voice configuration in the Foundry Agent

---

## Lab 7: Function Calling — Add a New Tool to the Voice Agent

**Duration**: 45 minutes  
**Objective**: Define a new tool in the Foundry Agent, implement its business logic, and wire it into the dispatch.

### 7.1 How Function Calling Works with Foundry Agents

In the Foundry Agent branch, tool definitions live in **two places**:

1. **AgentManager** (`scripts/AgentManager/Program.cs`) — Defines the tool schema (name, description, parameters) as `ResponseTool.CreateFunctionTool()`. This is uploaded to the Foundry Agent when you run `AgentManager create`.
2. **AzureVoiceLiveService** (`Services/AzureVoiceLiveService.cs`) — Dispatches tool calls to the implementation in `HandleFunctionCallAsync()`.
3. **AgentFunctions** (`Services/AgentFunctions.cs`) — Contains the actual business logic.

When the Foundry Agent decides to call a tool during a conversation:
1. Voice Live sends a `FunctionCallArgumentsDone` event to your app
2. Your app's `HandleFunctionCallAsync()` dispatches to the correct function
3. The result is sent back via `session.AddItemAsync(new FunctionCallOutputItem(...))`
4. The agent generates a spoken response based on the result

> **file_search is different**: The `file_search` tool (product catalog) is handled **server-side** by the Foundry platform. Your code never receives file_search invocations — the agent searches the vector store directly and uses the results in its response.

| Property | Purpose |
|---|---|
| `Name` | Function name (the model uses this to invoke it) |
| `Description` | Natural language description (the model reads this to decide **when** to call the function) |
| `Parameters` | JSON Schema defining the function's input parameters |

### 7.2 Exercise: Add a "check_weather" Tool

Let's add a weather lookup tool. We'll go through the three steps: define in AgentManager, implement, wire.

**Step 1: Define the tool in AgentManager** — Open `scripts/AgentManager/Program.cs` and add to the `GetFunctionTools()` method:

```csharp
ResponseTool.CreateFunctionTool(
    "check_weather",
    BinaryData.FromString(JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            city = new { type = "string", description = "The city name to check weather for" }
        },
        required = new[] { "city" }
    })),
    strictModeEnabled: null,
    functionDescription: "Check the current weather for a given city. Use when the caller asks about weather conditions."),
```

**Step 2: Implement the function** — Open `Services/AgentFunctions.cs` and add:

```csharp
private static readonly Dictionary<string, object> WeatherData = new(StringComparer.OrdinalIgnoreCase)
{
    ["New York"] = new { city = "New York", temperature = "72°F", condition = "Partly cloudy", humidity = "55%" },
    ["London"] = new { city = "London", temperature = "15°C", condition = "Rainy", humidity = "80%" },
    ["Berlin"] = new { city = "Berlin", temperature = "18°C", condition = "Sunny", humidity = "45%" },
    ["Tokyo"] = new { city = "Tokyo", temperature = "25°C", condition = "Clear", humidity = "60%" }
};

public static string CheckWeather(JsonElement args)
{
    var city = args.GetProperty("city").GetString() ?? "Unknown";

    if (WeatherData.TryGetValue(city, out var weather))
    {
        return JsonSerializer.Serialize(weather);
    }

    return JsonSerializer.Serialize(new { city, error = $"Weather data not available for {city}" });
}
```

**Step 3: Wire it into the dispatch** — Open `Services/AzureVoiceLiveService.cs` and add the case in `HandleFunctionCallAsync()`:

```csharp
result = functionName switch
{
    "customer_lookup" => AgentFunctions.LookupCustomer(args),
    "order_status" => AgentFunctions.CheckOrderStatus(args),
    "check_appointment" => AgentFunctions.CheckAppointment(args),
    "book_appointment" => AgentFunctions.BookAppointment(args),
    "cancel_appointment" => AgentFunctions.CancelAppointment(args),
    "search_knowledge_base" => AgentFunctions.SearchKnowledgeBase(args),
    "check_weather" => AgentFunctions.CheckWeather(args),          // <-- ADD THIS LINE
    "transfer_call" => await TransferCallAsync(args),
    "end_call" => await EndCallAsync(args),
    _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
};
```

**Step 4: Re-create the Foundry Agent** — The agent definition must be updated with the new tool:

```bash
# Delete the old agent version
dotnet run --project scripts/AgentManager -- delete

# Create a new version with the weather tool
dotnet run --project scripts/AgentManager -- create
```

Update `FoundryAgentVersion` in `appsettings.Development.json` with the new version number from the output.

### 7.3 Test Your New Tool

1. Restart the app: `dotnet run`
2. Call your ACS number
3. Say: *"What's the weather like in Tokyo?"*
4. The agent should call `check_weather` and tell you it's 25°C and clear
5. Try: *"How about London?"*

Watch the logs:
```
info: AzureVoiceLiveService[0] Function call: check_weather, args: {"city":"Tokyo"}
info: AzureVoiceLiveService[0] Function check_weather completed with result: {"city":"Tokyo","temperature":"25°C"...}
```

### 7.4 Update the System Prompt

Since we added a new capability, update `Prompts/system-prompt.txt` to mention it, then re-create the agent:

```
When you greet the caller, briefly introduce yourself and let them know you can help with:
- Looking up account information
- Checking order status
- Checking, booking or canceling appointments
- Searching the knowledge base
- Checking the weather                    <-- ADD THIS LINE
- Transferring to a human agent
- Ending the call
```

Then re-run AgentManager:  
```bash
dotnet run --project scripts/AgentManager -- delete
dotnet run --project scripts/AgentManager -- create
```

> **Key difference from main branch**: In the main branch, changing the system prompt only requires editing the file and restarting the app. In the Foundry Agent branch, you must also re-create the agent version since the prompt is baked into the agent definition.

### 7.5 Key Patterns for Writing Good Tools

| Principle | Why |
|---|---|
| **Clear description** | The model uses the description to decide when to invoke the tool. Be specific about what it does and when to use it |
| **Required parameters** | Mark parameters as `required` in the JSON Schema. Optional parameters should have sensible defaults |
| **Return JSON** | Always return JSON — the model parses it better than free-text |
| **Return useful errors** | Include an `error` field so the model can explain the failure to the caller |
| **Don't expose internals** | Don't return raw database IDs, stack traces, or connection strings |
| **Keep functions focused** | One function = one action. Don't create a "do_everything" function |
| **Three-file sync** | Tool definition (AgentManager) + implementation (AgentFunctions) + dispatch (AzureVoiceLiveService) must all agree |

### Lab 7 Checkpoint

- [ ] Added `check_weather` tool definition in AgentManager
- [ ] Implemented `CheckWeather()` in `AgentFunctions.cs`
- [ ] Added dispatch case in `HandleFunctionCallAsync()`
- [ ] Re-created the Foundry Agent with the new tool
- [ ] Updated `FoundryAgentVersion` in config
- [ ] Tested: agent correctly responds to weather questions
- [ ] Updated system prompt and re-created agent

---

## Lab 8: Deploy to Azure with `azd`

**Duration**: 45 minutes  
**Objective**: Deploy the voice agent to Azure App Service using the Azure Developer CLI.

### 8.1 What is `azd` (Azure Developer CLI)?

`azd` is a command-line tool that automates the "inner loop" of Azure development:

| Command | What it does |
|---|---|
| `azd init` | Initialize a project with an environment |
| `azd provision` | Create Azure infrastructure (from Bicep templates) |
| `azd deploy` | Build and deploy your code |
| `azd up` | `provision` + `deploy` in one step |
| `azd down` | Delete all provisioned resources |
| `azd env set KEY VALUE` | Set an environment variable for provisioning |

Configuration is defined in two files:
- **`azure.yaml`** — What to deploy (service name, host type, language)
- **`infra/main.bicep`** — What infrastructure to create

### 8.2 Understand the Infrastructure

Open `azure.yaml`:

```yaml
name: acs-voice-agent-foundry
metadata:
  template: acs-voice-agent-foundry
services:
  web:
    project: .
    host: appservice
    language: csharp
```

This tells `azd`: "Deploy the current directory as a C# app to Azure App Service."

The `azure.yaml` also defines **lifecycle hooks**:
- **`postprovision`** — Generates `appsettings.Development.json` from azd env vars and runs AgentManager to create the Foundry Agent (with retry logic)
- **`postdeploy`** — Updates the EventGrid subscription to point to the deployed App Service URL
- **`predown`** — Deletes the Foundry Agent and its resources before destroying infrastructure

Open `infra/main.bicep` — it creates:

| Resource | SKU | Why |
|---|---|---|
| Resource Group | — | Container for all resources |
| App Service Plan | B1 (Linux) | Hosting compute (Always On enabled) |
| App Service | .NET 8 | Hosts the voice agent (WebSockets enabled) |
| Azure AI Services | S0 | Hosts the AI models and Foundry project |
| AI Foundry Project | — | Organizes agents, deployments, and data |
| Model Deployment | gpt-4.1-mini | Chat model for the Foundry Agent |
| RBAC Assignments | — | Cognitive Services User for App Service MI and deployer |
| Log Analytics Workspace | — | Collects logs |
| Application Insights | — | Application monitoring and telemetry |

> **Key difference from main branch**: The Foundry branch Bicep creates AI Services, a Foundry Project, and model deployment automatically. The main branch only creates App Service and monitoring — AI resources are created manually.

### 8.3 Initialize and Configure the Environment

```bash
# Login to Azure (if not already)
azd auth login

# Initialize the environment (creates .azure/dev/ directory)
azd init -e dev
```

When prompted, select:
- **Use code in the current directory**: Yes
- **Confirm services**: Yes (it detects the ASP.NET application)

Now set the required environment variables:

```bash
# Required: ACS connection (pre-existing resource, not created by Bicep)
azd env set ACS_CONNECTION_STRING "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>"
azd env set ACS_RESOURCE_NAME "<your-acs-resource-name>"
azd env set ACS_RESOURCE_GROUP "<your-acs-resource-group>"

# Required: Your Azure user object ID (for RBAC)
azd env set AZURE_PRINCIPAL_ID "$(az ad signed-in-user show --query id -o tsv)"

# Optional: Override defaults
azd env set TRANSFER_PHONE_NUMBER "+441234567890"
azd env set CHAT_MODEL_NAME "gpt-4.1-mini"      # default: gpt-4.1-mini
azd env set AI_LOCATION "westeurope"              # default: same as AZURE_LOCATION
```

> `ACS_RESOURCE_NAME` and `ACS_RESOURCE_GROUP` are for the `postdeploy` EventGrid hook. If you prefer to manage EventGrid manually (as in Lab 5), you can skip these.

### 8.4 Provision and Deploy

```bash
azd up
```

This will:
1. **Provision** — Create the Resource Group, App Service, AI Services, Foundry Project, model deployment, RBAC, and monitoring
2. **Post-provision** — Generate `appsettings.Development.json` and run AgentManager to create the Foundry Agent
3. **Deploy** — Build the .NET app, package, and deploy to App Service

The process takes 5-10 minutes (longer than main branch due to AI resource provisioning). When complete, you'll see:

```
SUCCESS: Your application was provisioned and deployed to Azure in X minutes.

  SERVICE_WEB_URI: https://app-xxxxx.azurewebsites.net
```

**Copy the `SERVICE_WEB_URI`** — this is your production URL.

### 8.5 Verify Deployment

Open a browser and navigate to `https://app-xxxxx.azurewebsites.net/`:

You should see: **"ACS Voice Agent with Voice Live SDK"**

### 8.6 Update the EventGrid Webhook

Now that your app is deployed, you need to update the EventGrid subscription to point to your App Service URL instead of the dev tunnel:

1. Go to Azure Portal → your **ACS resource** → **Events**
2. Click the existing `incoming-call-webhook` subscription (created in Lab 5)
3. Click **Edit** and update the **Endpoint** to:
   ```
   https://app-xxxxx.azurewebsites.net/api/incomingCall
   ```
   (Replace `app-xxxxx` with your actual App Service hostname from step 8.5)
4. Click **Save**

EventGrid will re-validate the endpoint against your deployed app. Make sure the deployment completed successfully before updating.

> **Tip**: If you set `ACS_RESOURCE_NAME` and `ACS_RESOURCE_GROUP` in step 8.3, the `postdeploy` hook may have already done this automatically. Check the ACS Events blade to confirm.

### 8.7 Test in Production

Call your ACS phone number. The call now routes to your Azure App Service instead of your local machine. The experience should be identical to local testing.

### 8.8 View Logs

```bash
# Stream live logs from App Service
az webapp log tail --name <app-name> --resource-group <resource-group>
```

Or view in the Azure Portal → App Service → **Log stream**.

### 8.9 Useful `azd` Commands

| Command | When to use |
|---|---|
| `azd deploy` | Redeploy code changes (no infra changes) |
| `azd provision` | Apply infra changes only |
| `azd env list` | List all environments |
| `azd env get-values` | Show current env variables |
| `azd down` | **Delete everything** (resource group and all resources) |
| `azd monitor` | Open Application Insights dashboard in browser |
| `azd monitor --overview` | Open App Insights Overview blade |
| `azd monitor --live` | Open Live Metrics stream (great during test calls) |

### Lab 8 Checkpoint

- [ ] `azd init` completed with environment configured
- [ ] All environment variables set (including `AZURE_PRINCIPAL_ID`)
- [ ] `azd up` completed successfully (AI Services + Foundry Agent created)
- [ ] App accessible at the App Service URL
- [ ] EventGrid subscription updated to production URL
- [ ] Successfully called the agent via the cloud deployment

---

## Lab 9: Production Considerations — Scaling, Monitoring, and Content Safety

**Duration**: 30 minutes  
**Objective**: Understand what's needed to take this solution to production.

### 9.1 Scaling Architecture

The app uses in-process state (`ConcurrentDictionary<string, CallSession>`). A live call involves:
1. HTTP requests (EventGrid webhook, ACS callbacks)
2. A persistent WebSocket (bidirectional audio)
3. A Voice Live session (server connection to Microsoft Foundry)

All three are tied to the same process instance. If requests land on different instances, the call breaks. **Solution: ARR Affinity (sticky sessions).**

With App Service, ARR Affinity is enabled by default. ACS includes the session cookie on callback requests, routing them to the same instance.

### 9.2 Capacity Planning

| SKU | Concurrent Calls per Instance | Instances for ~174 Peak Concurrent |
|---|---|---|
| B1 (1 core, 1.75 GB) | ~50–80 | 3–4 |
| P1v3 (2 cores, 8 GB) | ~100–150 | 2 |
| P2v3 (4 cores, 16 GB) | ~200–300 | 1 |

> Formula: 20,000 calls/day × 5 min avg ÷ 1,440 min/day × 2.5 peak multiplier ≈ **174 peak concurrent calls**

### 9.3 Instance Failure Impact

If an instance crashes, all active calls on that instance are lost. The blast radius depends on instance count:

| Setup | Calls Lost per Failure |
|---|---|
| 1 large instance | All calls (~174) |
| 4 smaller instances | ~44 (~25%) |
| 8 smaller instances | ~22 (~12%) |

**Recommendation**: Run more smaller instances to minimize blast radius. WebSocket-based audio sessions cannot be recovered from a checkpoint — this is inherent to real-time streaming.

### 9.4 Key Production Settings

| Setting | Value | Why |
|---|---|---|
| Always On | `true` | Prevents cold starts when no traffic for 20+ minutes |
| WebSockets | `true` | Required for ACS media streaming |
| Health Check | `/` endpoint | Detects unhealthy instances for auto-replacement |
| Deployment Slots | Use staging + swap | Zero-downtime deployments (existing calls complete naturally on old slot) |
| Auto-scale | CPU > 70% → scale out | Handle traffic spikes automatically |

### 9.5 Monitoring with Application Insights

The app includes `Microsoft.ApplicationInsights.AspNetCore` and the Bicep templates create an Application Insights resource automatically.

**Key metrics to watch:**

| Metric | What it tells you | Alert threshold |
|---|---|---|
| Request duration (P95) | API response latency | > 5s |
| Failed requests | Errors in EventGrid/callback handling | > 1% |
| Live metrics | Real-time request flow | — (dashboard) |
| Custom events | Tool invocations, call durations | — (analytics) |

Access via:
```bash
azd monitor             # Opens Application Insights dashboard
azd monitor --overview  # Opens App Insights Overview blade
azd monitor --live      # Opens Live Metrics stream
```

### 9.6 Content Safety — Three Layers

Your voice agent has three layers of content safety protection:

**Layer 1: Microsoft Foundry Content Filters (Platform)**

Active by default on all Realtime audio models. Filters: Hate, Violence, Sexual, Self-Harm, Jailbreak, Protected Material. Zero code required.

**Layer 2: System Prompt (Model Behavior)**

The system prompt constrains the model's behavior:

```
RULES:
- NEVER speak the name of any tool or function out loud.
- When the caller wants to end the conversation, say a VERY brief goodbye and then invoke end_call.
- Always use the appropriate tool when the caller requests an action.
```

Add additional rules for your domain:
```
- Do not provide medical, legal, or financial advice
- If the caller is abusive, politely warn once, then end the call
- Do not discuss competitors or make promises about pricing
```


> **Strongly recommended**: For production voice agents, structure your system prompt following the [Realtime Prompting Guide](https://developers.openai.com/cookbook/examples/realtime_prompting_guide). This guide provides battle-tested patterns for real-time speech-to-speech models, including:
> - **Prompt structure**: Organize into labeled sections — Role & Objective, Personality & Tone, Instructions/Rules, Tools, Conversation Flow, Safety & Escalation
> - **Safety & Escalation**: Define explicit escalation triggers (threats, self-harm, repeated failures, user requests a human) with mandatory phrases and tool calls
> - **Conversation Flow**: Break the dialogue into goal-driven phases with clear exit criteria to prevent the model from stalling or skipping steps
> - **Tool call behavior**: Control when tools require confirmation vs. proactive execution, add preambles to mask latency
> - **Unclear audio handling**: Instruct the model to ask for clarification instead of guessing when audio is noisy or unintelligible
> - **Language constraints**: Pin the model to a target language to prevent accidental language switching
> - **Reduce repetition**: Add variety rules to avoid robotic-sounding repeated phrases
>
> A well-structured system prompt is your **most effective content safety control** — it defines what the model will and won't do before platform filters or code guardrails are even needed.
**Layer 3: Application Code (Guardrails)**

In `HandleFunctionCallAsync()`, you can add validation before executing any tool:

```csharp
// Example: limit transfer targets to approved numbers
if (functionName == "transfer_call")
{
    var target = args.GetProperty("target").GetString();
    if (!approvedTransferNumbers.Contains(target))
        return JsonSerializer.Serialize(new { error = "Transfer target not approved" });
}
```

### 9.7 Cost Estimation

For a single call (5 minutes average):

| Component | Approximate Cost per Call |
|---|---|
| ACS PSTN (inbound) | ~$0.02 |
| GPT Realtime Mini (audio tokens) | ~$0.05–$0.15 |
| App Service (B1 shared) | ~$0.001 |
| Application Insights | Negligible |
| **Total** | **~$0.07–$0.17 per call** |

At 20,000 calls/day: approximately $1,400–$3,400/month for the AI model + $400/month for PSTN.

> **Note**: Actual costs depend heavily on call duration, number of tool calls, and model output length. Run a pilot with real traffic to validate.

### 9.8 Cleaning Up (After Training)

To remove all Azure resources created during this training:

```bash
# Delete the azd-provisioned resources (App Service, AI Services, Foundry Project, monitoring)
# The predown hook will automatically delete the Foundry Agent and vector store
azd down --force

# Optionally delete the resource group with ACS (if created for training)
az group delete --name rg-voiceagent-training --yes
```

> **Warning**: `azd down` will delete the AI Services resource, Foundry Project, model deployment, and all agent versions. The `predown` hook cleans up the Foundry Agent resources first. Deleting the ACS resource group will also release your phone number permanently.

### Lab 9 Checkpoint

- [ ] Understand ARR Affinity and why it's needed
- [ ] Understand blast radius and instance failure impact
- [ ] Know the three content safety layers
- [ ] Can estimate costs for a production deployment
- [ ] Cleaned up resources (or noted them for cleanup later)

---

## Summary: What You Built Today

```
Phone Call -> ACS -> EventGrid -> ASP.NET Core -> Voice Live SDK -> Foundry Agent
                                    |                              |
                               Business Logic              Agent Definition
                          (client-side tools)          (tools, instructions, voice,
                                                        file_search, vector store)
```

### Key Technologies

| Technology | Role |
|---|---|
| **Microsoft Foundry** | Hosts the AI models and manages the Foundry Agent |
| **Foundry Agent** | Cloud-managed agent with tools, instructions, and file search |
| **Azure AI Voice Live** | Audio bridge between ACS and the Foundry Agent (agent mode) |
| **Azure Communication Services** | Connects to the phone network (PSTN) |
| **ASP.NET Core** | Bridges ACS audio with Voice Live — the "glue" |
| **AgentManager** | CLI tool for creating and versioning the Foundry Agent |
| **Azure Developer CLI (`azd`)** | Infrastructure-as-code provisioning and deployment |
| **Application Insights** | Production monitoring and diagnostics |

### Key Concepts

| Concept | What you learned |
|---|---|
| **Foundry Agent mode** | `SessionTarget.FromAgent` — agent defines tools, voice, and behavior; code handles audio only |
| **Realtime API** | Audio-native LLMs that process speech directly (no STT/TTS pipeline) |
| **Function calling** | Model decides when to invoke your business logic based on conversation context |
| **Client-side vs. server-side tools** | Function tools run in your code; file_search runs in the Foundry platform |
| **Voice Activity Detection** | How the model knows when the caller is done speaking |
| **Barge-in** | Caller can interrupt the agent mid-sentence |
| **Entra ID authentication** | `DefaultAzureCredential` — no API keys required |
| **Agent versioning** | AgentManager creates versioned agents; update config to switch versions |
| **`azd up`** | One command to provision infrastructure, create agent, and deploy code |
| **Content safety** | Platform filters + prompt rules + application guardrails |

### Next Steps

- **Connect real data sources**: Replace `AgentFunctions.cs` mock data with your CRM, ERP, or database APIs
- **Add more tools**: Follow the Lab 7 pattern (AgentManager + AgentFunctions + dispatch)
- **Add more documents**: Upload additional files to the vector store for richer product catalog search
- **Tune the prompt**: Edit `Prompts/system-prompt.txt` and re-run AgentManager
- **Enable Direct Routing**: Connect your enterprise SBC for internal phone numbers
- **Add post-call processing**: Call summarization, CRM updates, follow-up emails
- **Multi-language support**: Voice Live supports multiple languages — change the voice in the agent definition
- **Try the main branch**: Compare the direct mode (all-in-code) approach with the Foundry Agent approach

### Resources

| Resource | Link |
|---|---|
| Azure AI Voice Live SDK | [NuGet: Azure.AI.VoiceLive](https://www.nuget.org/packages/Azure.AI.VoiceLive) |
| ACS Call Automation Docs | [learn.microsoft.com/azure/communication-services/concepts/call-automation](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/call-automation) |
| GPT Realtime API | [learn.microsoft.com/azure/ai-services/openai/realtime-audio-quickstart](https://learn.microsoft.com/en-us/azure/ai-services/openai/realtime-audio-quickstart) |
| Azure Developer CLI | [learn.microsoft.com/azure/developer/azure-developer-cli](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/) |
| Azure Dev Tunnels | [learn.microsoft.com/azure/developer/dev-tunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) |
| Content Filtering | [learn.microsoft.com/azure/ai-services/openai/concepts/content-filter](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter) |
