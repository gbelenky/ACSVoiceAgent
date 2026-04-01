# Building a Real-Time AI Voice Agent with Azure

**One-Day Training Lab — Step by Step**

---

## Training Overview

| | |
|---|---|
| **Duration** | Full day (~7 hours with breaks) |
| **Level** | Intermediate — assumes Azure Portal familiarity and basic C# knowledge |
| **What you'll build** | A production-grade AI voice agent that answers phone calls, holds natural conversations, and executes business logic in real time |
| **What you'll learn** | Generative AI fundamentals, Azure AI Foundry, GPT Realtime API, Voice Live SDK, ACS telephony, function calling, and cloud deployment with `azd` |

### Agenda

| Time | Lab | Topic |
|---|---|---|
| 09:00–09:45 | 1 | Concepts — GenAI, LLMs, and the Realtime API |
| 09:45–10:30 | 2 | Azure AI Foundry — Create resources and deploy a Realtime model |
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

**Lab 2: Azure AI Foundry — Create Resources and Deploy a Realtime Model**
- 2.1 Create an Azure AI Foundry Resource
- 2.2 Deploy the Realtime Model
- 2.3 Record Your Credentials
- 2.4 Verify the Deployment

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
- 4.5 AzureVoiceLiveService — Voice Live Session Management
- 4.6 VoiceLiveSessionOptions — Session Configuration
- 4.7 AgentFunctions — Tool Implementations
- 4.8 CallSession and CallSessionManager — State Management
- 4.9 Key Takeaways

**Lab 5: Local Development — Dev Tunnels, Configuration, and First Call**
- 5.1 Install Dev Tunnels
- 5.2 Create and Start a Dev Tunnel
- 5.3 Configure appsettings.Development.json
- 5.4 Build and Run
- 5.5 Update the EventGrid Webhook
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
- 7.1 How Function Calling Works in Voice Live
- 7.2 Exercise: Add a `check_weather` Tool
- 7.3 The Tool Dispatch Pattern
- 7.4 Real vs. Simulated Tools — The Transfer Call Example
- 7.5 Key Takeaways

**Lab 8: Deploy to Azure with `azd`**
- 8.1 What is the Azure Developer CLI?
- 8.2 The azure.yaml File
- 8.3 The Bicep Infrastructure
- 8.4 Authenticate with Azure
- 8.5 Initialize the Project
- 8.6 Provision Infrastructure
- 8.7 Deploy the Application
- 8.8 Verify the Deployment
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

### 1.3 Azure AI Foundry and the GPT Realtime API

**Azure AI Foundry** (formerly Azure AI Studio / Azure OpenAI Service) is the Microsoft platform for deploying and managing AI models. It provides:

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

> **For this training, we use `gpt-realtime-mini`** — it provides excellent quality at lower cost and is the recommended starting point.

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
|          |     |                 |      |       App Service            |      |   Azure AI Foundry   |
|  Caller  |---->|  ACS            |----> |                              |----> |                      |
|  (Phone) |<----|  (PSTN Gateway) |<---- |  ASP.NET Core Minimal API    |<---- |  Voice Live Service  |
|          |     |                 |      |                              |      |  (gpt-realtime-mini) |
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

## Lab 2: Azure AI Foundry — Create Resources and Deploy a Realtime Model

**Duration**: 45 minutes  
**Objective**: Create an Azure AI Foundry resource and deploy the `gpt-realtime-mini` model.

### 2.1 Create an Azure AI Foundry Resource

1. Go to the [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → search for **Azure AI Services** (multi-service account)
3. Click **Create** and fill in:

   | Field | Value |
   |---|---|
   | **Subscription** | Your Azure subscription |
   | **Resource group** | Create new: `rg-voiceagent-training` |
   | **Region** | `West Europe` (or your preferred region — must support Realtime models) |
   | **Name** | `ai-voiceagent-<your-initials>` (must be globally unique) |
   | **Pricing tier** | Standard S0 |

4. Review and **Create**

> **Important**: The region must support GPT Realtime models. Check the [model availability table](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#model-summary-table-and-region-availability) if you're unsure. Common regions: `West Europe`, `East US 2`, `Sweden Central`.

### 2.2 Deploy the gpt-realtime-mini Model

1. Once your resource is created, go to **Azure AI Foundry Portal**: [ai.azure.com](https://ai.azure.com)
2. Select your subscription and resource
3. Navigate to **Deployments** → **+ Create deployment**
4. Configure:

   | Field | Value |
   |---|---|
   | **Model** | `gpt-realtime-mini` |
   | **Model version** | `2025-12-15` |
   | **Deployment name** | `gpt-realtime-mini` |
   | **Deployment type** | Global Standard |

5. Click **Deploy**

### 2.3 Gather Your Connection Details

After deployment, collect these values — you'll need them later:

| Value | Where to find it |
|---|---|
| **Endpoint** | Resource → Overview → Endpoint (e.g., `https://ai-voiceagent-gb.cognitiveservices.azure.com`) |
| **API Key** | Resource → Keys and Endpoint → Key 1 |
| **Model deployment name** | The name you chose above (e.g., `gpt-realtime-mini`) |

> **Write these down** or keep the portal tab open — you'll configure them in Lab 5.

### 2.4 Understanding Content Safety Filters

Your Azure AI Foundry resource comes with **built-in content safety filters** that are active by default for all Realtime audio models:

| Category | Default Threshold | What it filters |
|---|---|---|
| Hate & Fairness | Medium | Slurs, stereotypes, discriminatory content |
| Sexual | Medium | Explicit sexual content |
| Violence | Medium | Graphic violence, weapons instructions |
| Self-Harm | Medium | Self-harm instructions or encouragement |
| Jailbreak | On | Prompt injection attempts |
| Protected Material | On | Copyrighted text reproduction |

These filters work at the platform level — no code changes required. The model will refuse to generate harmful content regardless of what the caller asks.

### Lab 2 Checkpoint

- [ ] Azure AI Services resource created
- [ ] `gpt-realtime-mini` model deployed
- [ ] Endpoint URL and API key noted

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
   | **Country** | United States (or your preferred country) |
   | **Number type** | Toll-free (or Local, depending on availability) |
   | **Calling** | Make and receive calls |
   | **SMS** | None needed (optional) |

4. Select an available number and confirm purchase

> **Note**: Phone number availability varies by region. If no numbers are available in your preferred country, try United States toll-free numbers — they have the highest availability.

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
git clone https://github.com/gbelenky/ACSVoiceAgent.git
cd ACSVoiceAgent
dotnet restore
```

### 4.2 Project Structure

```
ACSVoiceAgent/
|-- Program.cs                          # Entry point -- endpoints, DI, WebSocket middleware
|-- Helper.cs                           # EventGrid JSON parsing utilities
|-- Models/
|   +-- CallSession.cs                  # Per-call state model
|-- Prompts/
|   +-- system-prompt.txt               # Agent personality and rules (plain text)
|-- Services/
|   |-- AcsMediaStreamingHandler.cs     # ACS WebSocket send/receive
|   |-- AgentFunctions.cs               # Business logic tools (simulated data)
|   |-- AzureVoiceLiveService.cs        # Voice Live session management + tool dispatch
|   +-- CallSessionManager.cs           # In-process session tracking
|-- infra/                              # Bicep IaC for Azure deployment
|-- appsettings.json                    # Configuration (defaults)
|-- appsettings.Development.json        # Local dev config (secrets -- not in source control)
|-- azure.yaml                          # Azure Developer CLI project definition
+-- ACSVoiceAgent.csproj                # .NET 8 project file
```

### 4.3 NuGet Packages

Open `ACSVoiceAgent.csproj` and review the dependencies:

| Package | Version | Purpose |
|---|---|---|
| `Azure.AI.VoiceLive` | 1.0.0 | Voice Live SDK — manages the connection to GPT Realtime via Azure AI Foundry |
| `Azure.Communication.CallAutomation` | 1.5.1 | ACS Call Automation — answer, transfer, hang up calls programmatically |
| `Azure.Messaging.EventGrid` | 5.0.0 | Parse EventGrid events (IncomingCall triggers) |
| `Azure.Identity` | 1.19.0 | Azure authentication (for production with Managed Identity) |
| `Microsoft.ApplicationInsights.AspNetCore` | 2.22.0 | Monitoring and telemetry |

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
3. Create `AzureVoiceLiveService` (bridges to Azure AI Foundry)
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

### 4.6 AzureVoiceLiveService — The Core Bridge

Open `Services/AzureVoiceLiveService.cs`. This is the most important file.

**CreateSessionAsync()** — Sets up the Voice Live connection:

1. Reads config (endpoint, API key, model)
2. Loads the system prompt from `Prompts/system-prompt.txt`
3. Creates `VoiceLiveSessionOptions` with:
   - Model, instructions, voice (`en-US-Ava:DragonHDLatestNeural`)
   - Audio format (PCM 16-bit), noise reduction, echo cancellation
   - VAD settings (threshold, prefix padding, silence duration)
   - **8 tool definitions** (`VoiceLiveFunctionDefinition`)
4. Connects: `client.StartSessionAsync(sessionOptions)`
5. Starts background event loop: `Task.Run(ReceiveEventsAsync)`
6. Triggers the agent's greeting: `session.StartResponseAsync()`

**ReceiveEventsAsync()** — Processes server events:

```csharp
await foreach (var serverEvent in _session.GetUpdatesAsync(cancellationToken))
{
    await HandleSessionUpdateAsync(serverEvent, cancellationToken);
}
```

This is an async stream — events arrive continuously for the duration of the call.

**HandleSessionUpdateAsync()** — Routes events:

| Event | Action |
|---|---|
| `AudioDelta` | Forward audio bytes to ACS (the agent is speaking) |
| `SpeechStarted` | Send `StopAudio` to ACS (barge-in — caller interrupted) |
| `FunctionCallArgumentsDone` | Execute the tool and send result back |
| `AudioTranscriptDone` | Log what the agent said |
| `ResponseDone` | If end-call was requested, trigger hang-up |

**HandleFunctionCallAsync()** — Tool dispatch:

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

The pattern is: execute the function → send the result to Voice Live → tell the model to generate a response.

### 4.7 AgentFunctions — Simulated Business Logic

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

### 4.8 AcsMediaStreamingHandler — WebSocket Wrapper

Open `Services/AcsMediaStreamingHandler.cs`.

Simple wrapper over the ACS WebSocket:

- `SendMessageAsync()` — Sends JSON to ACS (outbound audio or stop command)
- `ReceiveMessageAsync()` — Reads JSON from ACS (inbound caller audio)
- `OutStreamingData.GetAudioDataForOutbound()` — Wraps PCM bytes in ACS JSON format
- `OutStreamingData.GetStopAudioForOutbound()` — Sends stop command (for barge-in)

### 4.9 System Prompt

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
- Sets the agent's personality (friendly, concise)
- Establishes rules (never say tool names, use tools proactively)
- Loaded at runtime from the file system — edit and restart to apply changes

### Lab 4 Review Questions

1. How does `CallSessionManager` bridge the gap between the HTTP callback and the WebSocket?
2. What happens in `HandleSessionUpdateAsync()` when the model emits an `AudioDelta` event?
3. Why does `end_call` not immediately hang up the phone?
4. What does `session.StartResponseAsync()` do, and when is it called?

---

## Lab 5: Local Development — Dev Tunnels, Configuration, and First Call

**Duration**: 45 minutes  
**Objective**: Configure the app, start it locally, and make your first AI voice call.

### 5.1 Create a Dev Tunnel

Your local app runs on `localhost:5000`, but ACS needs a public URL of your app to send events and connect WebSockets. A **dev tunnel** creates a secure public URL that forwards traffic to your local machine.

```bash
# Login to Azure (if not already logged in)
az login

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

### 5.2 Configure Application Settings

Create `appsettings.Development.json` in the project root (this file is in `.gitignore` and won't be committed):

```json
{
  "DevTunnelUri": "https://<your-tunnel-url>.devtunnels.ms",
  "AcsConnectionString": "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>",
  "AzureVoiceLiveApiKey": "<your-api-key-from-lab-2>",
  "AzureVoiceLiveEndpoint": "https://<your-ai-resource>.cognitiveservices.azure.com",
  "VoiceLiveModel": "gpt-realtime-mini"
}
```

Fill in the values from Labs 2 and 3:

| Setting | Source |
|---|---|
| `DevTunnelUri` | Your dev tunnel URL from step 5.1 |
| `AcsConnectionString` | ACS resource → Keys (Lab 3) |
| `AzureVoiceLiveApiKey` | AI Foundry resource → Keys and Endpoint (Lab 2) |
| `AzureVoiceLiveEndpoint` | AI Foundry resource → Overview → Endpoint (Lab 2) |
| `VoiceLiveModel` | Your model deployment name: `gpt-realtime-mini` (Lab 2) |

### 5.3 Register the EventGrid Webhook

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

EventGrid will send a validation request to your endpoint. The app handles this automatically — but the app needs to be running first. So let's start it.

### 5.4 Build and Run

```bash
cd ACSVoiceAgent
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

### 5.5 Make Your First Call

1. **Pick up your phone** and dial the ACS phone number you acquired in Lab 3
2. Watch the terminal logs — you should see:

   ```
   info: Program[0] Incoming Call event received
   info: Program[0] Incoming call from +1234567890
   info: Program[0] Answered call. Connection ID: ...
   info: Program[0] Call connected. ConnectionId: ...
   info: Program[0] ACS WebSocket connected
   info: AzureVoiceLiveService[0] Connecting to Voice Live with 8 tools...
   info: AzureVoiceLiveService[0] Connected to Voice Live successfully
   ```

3. **Listen** — the agent should greet you: "Hi, I'm Ava, your customer care assistant..."

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

### 5.6 Understanding the Logs

While on the call, watch the terminal output. Key log lines to look for:

```
# Voice Live connected and agent starts speaking
info: AzureVoiceLiveService[0] Voice Live session created
info: AzureVoiceLiveService[0] Agent transcript: Hi! I'm Ava...

# Caller speaks and a tool is invoked
info: AzureVoiceLiveService[0] Speech started (barge-in)
info: AzureVoiceLiveService[0] Function call: customer_lookup with call_id: call_xxx, args: {"identifier":"12345"}
info: AzureVoiceLiveService[0] Function customer_lookup completed with result: {"name":"Genady Belenky","id":"12345","phone":"+14255551234","tier":"Gold","balance":150.00}

# Call ends
warn: AzureVoiceLiveService[0] End call requested for ...
warn: AzureVoiceLiveService[0] Waiting 2s before hanging up call ...
warn: AzureVoiceLiveService[0] HangUpAsync completed successfully
info: Program[0] Call disconnected. CorrelationId: ...
```

### 5.7 Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| No logs when calling | EventGrid webhook not registered or wrong URL | Re-check the endpoint URL includes `/api/incomingCall` |
| "Incoming Call event received" but no answer | ACS connection string wrong | Verify `AcsConnectionString` in config |
| Call answers but no audio/greeting | Voice Live connection failed | Check `AzureVoiceLiveEndpoint` and `AzureVoiceLiveApiKey` |
| "No call session found after 30s" | Race condition (rare) | Retry the call — the Channel handoff may have timed out |
| Tunnel errors | Dev tunnel not running | Restart `devtunnel host` and update the config URL |

### Lab 5 Checkpoint

- [ ] Dev tunnel running with public URL
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
// Client: connects to Azure AI Foundry
var client = new VoiceLiveClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

// Session: represents one active conversation
var session = await client.StartSessionAsync(sessionOptions, cancellationToken);
```

The **client** is the connection factory. The **session** is the active conversation — it holds the audio stream, the conversation history, and the tool state. One session per phone call.

### 6.2 Session Options Deep Dive — VoiceLiveSessionOptions

`VoiceLiveSessionOptions` is the central configuration object for a Voice Live session. Every property controls a different aspect of how the realtime model processes audio, generates responses, and interacts with your application. Below is the complete reference for every property, grouped by category.

#### Session Identity

| Property | Type | Description |
|---|---|---|
| `Model` | `string` | The deployment name of the realtime model to use. Must match the deployment name in your Azure AI Foundry resource (e.g., `gpt-realtime-mini`). This is **not** the catalog model name — it's the name you chose when deploying. |
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

#### How Our App Configures the Session

Here's the actual code from `AzureVoiceLiveService.cs` with annotations for each property:

```csharp
var sessionOptions = new VoiceLiveSessionOptions
{
    // Session Identity
    Model = model,                                     // "gpt-realtime-mini" from config
    Instructions = systemPrompt,                       // Loaded from Prompts/system-prompt.txt

    // Voice
    Voice = new AzureStandardVoice("en-US-Ava:DragonHDLatestNeural"),

    // Audio Format (matching ACS PCM 24kHz mono)
    InputAudioFormat = InputAudioFormat.Pcm16,
    OutputAudioFormat = OutputAudioFormat.Pcm16,

    // Telephony Audio Processing (Voice Live exclusive)
    InputAudioNoiseReduction = new AudioNoiseReduction(AudioNoiseReductionType.FarField),
    InputAudioEchoCancellation = new AudioEchoCancellation(),

    // Turn Detection
    TurnDetection = new ServerVadTurnDetection
    {
        Threshold = 0.5f,                              // Balanced sensitivity
        PrefixPadding = TimeSpan.FromMilliseconds(300),// Capture word beginnings
        SilenceDuration = TimeSpan.FromMilliseconds(500)// Wait 500ms of silence
    }
};

// Modalities: both text and audio
sessionOptions.Modalities.Clear();
sessionOptions.Modalities.Add(InteractionModality.Text);
sessionOptions.Modalities.Add(InteractionModality.Audio);

// Tools: 8 function definitions added to sessionOptions.Tools
// (customer_lookup, order_status, check_appointment, book_appointment,
//  cancel_appointment, search_knowledge_base, transfer_call, end_call)
```

> **Key takeaway**: `VoiceLiveSessionOptions` is where you configure _everything_ about a voice session — the model, the voice, how audio is processed, when the agent starts speaking, and what tools are available. Getting these settings right is the difference between an agent that feels natural and one that frustrates callers.

### 6.3 Voice Options

Voice Live supports **all Azure TTS voices**, not just the 6 built-in OpenAI voices:

| Category | Example | SDK Class |
|---|---|---|
| Azure Standard/HD | `en-US-Ava:DragonHDLatestNeural` | `AzureStandardVoice` |
| Azure Custom Neural | Your custom-trained voice | `AzureCustomVoice` |

The `DragonHD` voices are the latest generation — more natural, lower latency for real-time use.

Browse the full voice catalog: [Azure TTS Voice Gallery](https://speech.microsoft.com/portal/voicegallery)

### 6.4 Voice Activity Detection (VAD) Tuning

VAD controls how the model decides when the caller has stopped speaking. This is critical for conversational quality.

**Experiment**: While your app is running, try these changes in `AzureVoiceLiveService.cs`:

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

> **Important**: These are C# code changes, not runtime configuration. You must stop the app (`Ctrl+C`), then restart with `dotnet run` for changes to take effect. The session options are compiled into the binary and constructed fresh for each new incoming call — existing active calls are not affected.

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

Try changing the voice to a different Azure HD voice:

1. Open `Services/AzureVoiceLiveService.cs`
2. Find the `Voice` property in `CreateSessionAsync()`
3. Change it:

```csharp
// Try a male voice:
Voice = new AzureStandardVoice("en-US-Andrew:DragonHDLatestNeural"),

// Try a British accent:
Voice = new AzureStandardVoice("en-GB-Libby:DragonHDLatestNeural"),
```

4. Restart and call in to hear the difference

### Lab 6 Checkpoint

- [ ] Understand VoiceLiveClient vs. VoiceLiveSession
- [ ] Experimented with VAD SilenceDuration
- [ ] Understand barge-in mechanism
- [ ] Changed the voice to a different Azure voice

---

## Lab 7: Function Calling — Add a New Tool to the Voice Agent

**Duration**: 45 minutes  
**Objective**: Define a new tool, implement its business logic, and wire it into the voice agent.

### 7.1 Understanding Tool Definitions

Each tool is defined as a `VoiceLiveFunctionDefinition` with:

| Property | Purpose |
|---|---|
| `Name` | Function name (the model uses this to invoke it) |
| `Description` | Natural language description (the model reads this to decide **when** to call the function) |
| `Parameters` | JSON Schema defining the function's input parameters |

The **description is critical** — it's what the model uses to decide when to invoke the tool. A vague description leads to unreliable tool selection.

### 7.2 Exercise: Add a "check_weather" Tool

Let's add a weather lookup tool. We'll go through the three steps: define, implement, wire.

**Step 1: Define the tool** — Open `Services/AzureVoiceLiveService.cs` and add the tool definition in `CreateSessionAsync()`, after the existing tools:

```csharp
sessionOptions.Tools.Add(new VoiceLiveFunctionDefinition("check_weather")
{
    Description = "Check the current weather for a given city. Use when the caller asks about weather conditions.",
    Parameters = BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "city": { 
                    "type": "string", 
                    "description": "The city name to check weather for" 
                }
            },
            "required": ["city"]
        }
        """)
});
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

Since we added a new capability, update `Prompts/system-prompt.txt` to mention it:

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

### 7.5 Key Patterns for Writing Good Tools

| Principle | Why |
|---|---|
| **Clear description** | The model uses the description to decide when to invoke the tool. Be specific about what it does and when to use it |
| **Required parameters** | Mark parameters as `required` in the JSON Schema. Optional parameters should have sensible defaults |
| **Return JSON** | Always return JSON — the model parses it better than free-text |
| **Return useful errors** | Include an `error` field in the response so the model can explain the failure to the caller |
| **Don't expose internals** | Don't return raw database IDs, stack traces, or connection strings. Return user-friendly data only |
| **Keep functions focused** | One function = one action. Don't create a "do_everything" function |

### Lab 7 Checkpoint

- [ ] Added `check_weather` tool definition in Voice Live session options
- [ ] Implemented `CheckWeather()` in `AgentFunctions.cs`
- [ ] Added dispatch case in `HandleFunctionCallAsync()`
- [ ] Tested: agent correctly responds to weather questions
- [ ] Updated system prompt with the new capability

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
name: acs-voice-agent
metadata:
  template: acs-voice-agent
services:
  web:
    project: .
    host: appservice
    language: csharp
```

This tells `azd`: "Deploy the current directory as a C# app to Azure App Service."

Open `infra/main.bicep` — it creates:

| Resource | SKU | Why |
|---|---|---|
| Resource Group | — | Container for all resources |
| App Service Plan | B1 (Linux) | Hosting compute (Always On enabled) |
| App Service | .NET 8 | Hosts the voice agent (WebSockets enabled) |
| Log Analytics Workspace | — | Collects logs |
| Application Insights | — | Application monitoring and telemetry |

**Key App Service settings configured by Bicep:**
- `alwaysOn: true` — prevents cold starts (critical for real-time calls)
- `webSocketsEnabled: true` — required for ACS media streaming
- All config values (API keys, endpoints) injected as app settings

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
azd env set AZURE_LOCATION westeurope
azd env set ACS_CONNECTION_STRING "endpoint=https://<your-acs>.communication.azure.com/;accesskey=<key>"
azd env set AZURE_VOICE_LIVE_API_KEY "<your-api-key>"
azd env set AZURE_VOICE_LIVE_ENDPOINT "https://<your-resource>.cognitiveservices.azure.com"
azd env set VOICE_LIVE_MODEL "gpt-realtime-mini"
azd env set TRANSFER_PHONE_NUMBER "+1234567890"
```

> Replace the placeholder values with your actual values from Labs 2 and 3.

### 8.4 Provision and Deploy

```bash
azd up
```

This will:
1. **Provision** — Create the Resource Group, App Service Plan, App Service, Log Analytics, and Application Insights
2. **Deploy** — Build the .NET app, package, and deploy to App Service

The process takes 3-5 minutes. When complete, you'll see:

```
SUCCESS: Your application was provisioned and deployed to Azure in X minutes.

  SERVICE_WEB_URI: https://app-xxxxx.azurewebsites.net
```

**Copy the `SERVICE_WEB_URI`** — this is your production URL.

### 8.5 Verify Deployment

Open a browser and navigate to `https://app-xxxxx.azurewebsites.net/`:

You should see: **"ACS Voice Agent with Voice Live SDK"**

### 8.6 Update the EventGrid Webhook

Now switch your ACS EventGrid subscription from the dev tunnel to the production URL:

1. Go to Azure Portal → ACS resource → **Events**
2. Click on your existing event subscription
3. Change the endpoint URL to:
   ```
   https://app-xxxxx.azurewebsites.net/api/incomingCall
   ```
4. Save

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
- [ ] All environment variables set
- [ ] `azd up` completed successfully
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
3. A Voice Live session (server connection to Azure AI Foundry)

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

**Layer 1: Azure AI Foundry Content Filters (Platform)**

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
# Delete the azd-provisioned resources (App Service, monitoring)
azd down --force

# Optionally delete the resource group with ACS and AI Foundry
az group delete --name rg-voiceagent-training --yes
```

> **Warning**: Deleting the resource group will delete your ACS resource, phone number, and AI Foundry resource permanently.

### Lab 9 Checkpoint

- [ ] Understand ARR Affinity and why it's needed
- [ ] Understand blast radius and instance failure impact
- [ ] Know the three content safety layers
- [ ] Can estimate costs for a production deployment
- [ ] Cleaned up resources (or noted them for cleanup later)

---

## Summary: What You Built Today

```
Phone Call -> ACS -> EventGrid -> ASP.NET Core -> Voice Live SDK -> GPT Realtime Mini
                                    |                              |
                               Business Logic              Audio Processing
                            (8 function tools)          (noise reduction, VAD,
                                                        echo cancellation, HD voice)
```

### Key Technologies

| Technology | Role |
|---|---|
| **Azure AI Foundry** | Hosts the GPT Realtime model |
| **Azure AI Voice Live** | Adds telephony-grade audio features (noise, echo, HD voice, VAD) |
| **Azure Communication Services** | Connects to the phone network (PSTN) |
| **ASP.NET Core** | Bridges ACS audio with Voice Live — the "glue" |
| **Azure Developer CLI (`azd`)** | Infrastructure-as-code provisioning and deployment |
| **Application Insights** | Production monitoring and diagnostics |

### Key Concepts

| Concept | What you learned |
|---|---|
| **Realtime API** | Audio-native LLMs that process speech directly (no STT/TTS pipeline) |
| **Function calling** | Model decides when to invoke your business logic based on conversation context |
| **Voice Activity Detection** | How the model knows when the caller is done speaking |
| **Barge-in** | Caller can interrupt the agent mid-sentence |
| **Bidirectional WebSocket** | Real-time audio streaming between ACS and your app |
| **Channel-based coordination** | Thread-safe session handoff between HTTP callbacks and WebSockets |
| **`azd up`** | One command to provision infrastructure and deploy code |
| **Content safety** | Platform filters + prompt rules + application guardrails |

### Next Steps

- **Connect real data sources**: Replace `AgentFunctions.cs` mock data with your CRM, ERP, or database APIs
- **Add more tools**: Follow the Lab 7 pattern to add domain-specific capabilities
- **Tune the prompt**: Iterate on `Prompts/system-prompt.txt` for your brand voice and policies
- **Enable Direct Routing**: Connect your enterprise SBC for internal phone numbers
- **Add post-call processing**: Call summarization, CRM updates, follow-up emails (good candidates for Durable Functions)
- **Multi-language support**: Voice Live supports multiple languages — change the voice and prompt

### Resources

| Resource | Link |
|---|---|
| Azure AI Voice Live SDK | [NuGet: Azure.AI.VoiceLive](https://www.nuget.org/packages/Azure.AI.VoiceLive) |
| ACS Call Automation Docs | [learn.microsoft.com/azure/communication-services/concepts/call-automation](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/call-automation) |
| GPT Realtime API | [learn.microsoft.com/azure/ai-services/openai/realtime-audio-quickstart](https://learn.microsoft.com/en-us/azure/ai-services/openai/realtime-audio-quickstart) |
| Azure Developer CLI | [learn.microsoft.com/azure/developer/azure-developer-cli](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/) |
| Azure Dev Tunnels | [learn.microsoft.com/azure/developer/dev-tunnels](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview) |
| Content Filtering | [learn.microsoft.com/azure/ai-services/openai/concepts/content-filter](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter) |
