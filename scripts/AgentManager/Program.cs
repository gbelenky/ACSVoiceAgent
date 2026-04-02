// Azure AI Foundry Standard Agent Manager for Voice Live Agent
//
// Creates and deletes the Voice Live agent with:
// - FunctionTool for all business logic (customer lookup, orders, appointments, FAQ, call control)
// - FileSearchTool + vector store for product catalog (RAG pattern — server-side)
//
// Usage:
//   dotnet run --project scripts/AgentManager -- create
//   dotnet run --project scripts/AgentManager -- delete

using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;

const string AgentName = "VoiceLiveAgent";

if (args.Length < 1)
{
    Console.WriteLine("Usage: AgentManager <command>");
    Console.WriteLine("Commands:");
    Console.WriteLine("  create  - Create agent with vector store and function tools");
    Console.WriteLine("  delete  - Delete the agent and associated resources");
    return 1;
}

var command = args[0].ToLowerInvariant();
return command switch
{
    "create" => await CreateAgentAsync(),
    "delete" => await DeleteAgentAsync(),
    _ => Error($"Unknown command: {command}")
};

// ---------------------------------------------------------------------------
// Create
// ---------------------------------------------------------------------------
async Task<int> CreateAgentAsync()
{
    Console.WriteLine("Creating Voice Live Standard Agent...");

    var modelDeployment = Environment.GetEnvironmentVariable("CHAT_MODEL_DEPLOYMENT") ?? "gpt-4.1-mini";
    var instructions = LoadInstructions();
    var projectClient = GetProjectClient();

    var filesClient = projectClient.ProjectOpenAIClient.GetProjectFilesClient();
    var vectorStoresClient = projectClient.ProjectOpenAIClient.GetProjectVectorStoresClient();

    // Step 1: Upload product catalog document
    Console.WriteLine("  Uploading product catalog document...");
    var docsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "product-catalog.md");
    docsPath = Path.GetFullPath(docsPath);
    if (!File.Exists(docsPath))
    {
        // Fallback: resolve relative to the project source directory
        docsPath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "docs", "product-catalog.md"));
    }
    if (!File.Exists(docsPath))
    {
        Console.Error.WriteLine($"Document not found: {docsPath}");
        return 1;
    }

    var uploadedFile = await filesClient.UploadFileAsync(docsPath, FileUploadPurpose.Assistants);
    var fileId = uploadedFile.Value.Id;
    Console.WriteLine($"  FILE_ID: {fileId}");

    // Step 2: Create vector store and add the file
    Console.WriteLine("  Creating vector store...");
    var vectorStore = await vectorStoresClient.CreateVectorStoreAsync(
        new VectorStoreCreationOptions { Name = "ProductCatalog" });
    var vectorStoreId = vectorStore.Value.Id;
    Console.WriteLine($"  VECTORSTORE_ID: {vectorStoreId}");

    await vectorStoresClient.AddFileToVectorStoreAsync(vectorStoreId, fileId);

    // Wait for vector store processing
    Console.WriteLine("  Waiting for vector store processing...");
    while (true)
    {
        var status = await vectorStoresClient.GetVectorStoreAsync(vectorStoreId);
        if (status.Value.Status == VectorStoreStatus.Completed)
        {
            Console.WriteLine("  Vector store ready!");
            break;
        }
        if (status.Value.Status == VectorStoreStatus.Expired)
        {
            Console.Error.WriteLine("  Vector store processing failed.");
            return 1;
        }
        await Task.Delay(1000);
    }

    // Step 3: Build tools list — FunctionTool instances (client-side) + FileSearchTool (server-side)
    Console.WriteLine("  Creating Standard Agent...");

    var functionTools = GetFunctionTools();
    var tools = new List<ResponseTool>(functionTools)
    {
        ResponseTool.CreateFileSearchTool([vectorStoreId])
    };

    var agentDefinition = new DeclarativeAgentDefinition(modelDeployment)
    {
        Instructions = instructions,
    };
    foreach (var tool in tools)
        agentDefinition.Tools.Add(tool);

    var agent = await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
        AgentName, new ProjectsAgentVersionCreationOptions(agentDefinition));

    var agentId = agent.Value.Id;
    Console.WriteLine($"  AGENT_ID: {agentId}");
    Console.WriteLine();
    Console.WriteLine(new string('=', 50));
    Console.WriteLine("Voice Live Standard Agent created successfully!");
    Console.WriteLine($"  Agent ID:        {agentId}");
    Console.WriteLine($"  Vector Store ID: {vectorStoreId}");
    Console.WriteLine($"  File ID:         {fileId}");
    Console.WriteLine($"  Function tools:  {functionTools.Count} (client-side)");
    Console.WriteLine($"  file_search:     product catalog (server-side)");
    Console.WriteLine(new string('=', 50));
    return 0;
}

// ---------------------------------------------------------------------------
// Delete
// ---------------------------------------------------------------------------
async Task<int> DeleteAgentAsync()
{
    Console.WriteLine("Deleting Voice Live Standard Agent and resources...");

    var agentId = Environment.GetEnvironmentVariable("AGENT_ID");
    var vectorStoreId = Environment.GetEnvironmentVariable("VECTORSTORE_ID");
    var fileId = Environment.GetEnvironmentVariable("FILE_ID");

    if (string.IsNullOrEmpty(agentId))
    {
        Console.WriteLine("No AGENT_ID found in environment. Skipping deletion.");
        return 0;
    }

    var projectClient = GetProjectClient();
    var filesClient = projectClient.ProjectOpenAIClient.GetProjectFilesClient();
    var vectorStoresClient = projectClient.ProjectOpenAIClient.GetProjectVectorStoresClient();

    // Delete agent version
    string agentName, agentVersion;
    if (agentId.Contains(':'))
    {
        var lastColon = agentId.LastIndexOf(':');
        agentName = agentId[..lastColon];
        agentVersion = agentId[(lastColon + 1)..];
    }
    else
    {
        agentName = agentId;
        agentVersion = "1";
    }

    try
    {
        Console.WriteLine($"  Deleting agent: {agentName} version {agentVersion}");
        await projectClient.AgentAdministrationClient.DeleteAgentVersionAsync(agentName, agentVersion);
        Console.WriteLine("  Agent deleted.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Warning: Could not delete agent: {ex.Message}");
    }

    // Delete vector store
    if (!string.IsNullOrEmpty(vectorStoreId))
    {
        try
        {
            Console.WriteLine($"  Deleting vector store: {vectorStoreId}");
            await vectorStoresClient.DeleteVectorStoreAsync(vectorStoreId);
            Console.WriteLine("  Vector store deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Could not delete vector store: {ex.Message}");
        }
    }

    // Delete file
    if (!string.IsNullOrEmpty(fileId))
    {
        try
        {
            Console.WriteLine($"  Deleting file: {fileId}");
            await filesClient.DeleteFileAsync(fileId);
            Console.WriteLine("  File deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Could not delete file: {ex.Message}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("Cleanup completed.");
    return 0;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------
AIProjectClient GetProjectClient()
{
    var endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")
        ?? throw new InvalidOperationException("PROJECT_ENDPOINT environment variable is required");
    return new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());
}

string LoadInstructions()
{
    // Try relative to the binary output directory first, then relative to solution root
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Prompts", "system-prompt.txt")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Prompts", "system-prompt.txt")),
    };

    foreach (var path in candidates)
    {
        if (File.Exists(path))
            return File.ReadAllText(path).Trim();
    }

    throw new FileNotFoundException(
        $"Instructions file not found. Searched: {string.Join(", ", candidates)}");
}

List<ResponseTool> GetFunctionTools()
{
    return
    [
        ResponseTool.CreateFunctionTool(
            "customer_lookup",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    identifier = new { type = "string", description = "Customer ID or phone number in E.164 format" }
                },
                required = new[] { "identifier" }
            })),
            strictModeEnabled: null, functionDescription: "Look up customer information by customer ID or phone number"),

        ResponseTool.CreateFunctionTool(
            "order_status",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    order_id = new { type = "string", description = "The order ID to check status for" }
                },
                required = new[] { "order_id" }
            })),
            strictModeEnabled: null, functionDescription: "Check the status of an order by order ID"),

        ResponseTool.CreateFunctionTool(
            "check_appointment",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    customer_id = new { type = "string", description = "The customer ID to check appointments for" }
                },
                required = new[] { "customer_id" }
            })),
            strictModeEnabled: null, functionDescription: "Check existing appointments for a customer"),

        ResponseTool.CreateFunctionTool(
            "book_appointment",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    customer_id = new { type = "string", description = "The customer ID" },
                    date = new { type = "string", description = "Appointment date (YYYY-MM-DD)" },
                    time = new { type = "string", description = "Appointment time (HH:MM)" }
                },
                required = new[] { "customer_id" }
            })),
            strictModeEnabled: null, functionDescription: "Book a new appointment for a customer"),

        ResponseTool.CreateFunctionTool(
            "cancel_appointment",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    appointment_id = new { type = "string", description = "The appointment ID to cancel" }
                },
                required = new[] { "appointment_id" }
            })),
            strictModeEnabled: null, functionDescription: "Cancel an existing appointment"),

        ResponseTool.CreateFunctionTool(
            "search_knowledge_base",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "The search query" }
                },
                required = new[] { "query" }
            })),
            strictModeEnabled: null, functionDescription: "Search the FAQ knowledge base for information about policies, shipping, returns, etc."),

        ResponseTool.CreateFunctionTool(
            "transfer_call",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    reason = new { type = "string", description = "Reason for the transfer" }
                }
            })),
            strictModeEnabled: null, functionDescription: "Transfer the current call to another phone number or department"),

        ResponseTool.CreateFunctionTool(
            "end_call",
            BinaryData.FromString(JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    reason = new { type = "string", description = "Reason for ending the call" }
                }
            })),
            strictModeEnabled: null, functionDescription: "End the current phone call. Call this tool IMMEDIATELY when the caller wants to hang up — do NOT speak first."),
    ];
}

int Error(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
