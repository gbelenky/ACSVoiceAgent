"""
Azure AI Foundry Standard Agent Manager for Voice Live Agent

Creates and deletes the Voice Live agent with:
- FunctionTool for all business logic (customer lookup, orders, appointments, FAQ, call control)
- FileSearchTool + vector store for product catalog (RAG pattern — server-side)

The function tools are dispatched client-side by the C# app (AgentFunctions.cs + ACS APIs).
The product catalog tool uses file_search — no client code needed.

Usage:
    python agent_manager.py create   - Create agent with all tools + vector store
    python agent_manager.py delete   - Delete the agent and associated resources
"""

import os
import sys
import time
from pathlib import Path

from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    PromptAgentDefinition,
    FileSearchTool,
    FunctionTool,
    FunctionDefinition,
)

AGENT_NAME = "VoiceLiveAgent"


def get_project_client() -> AIProjectClient:
    endpoint = os.environ.get("PROJECT_ENDPOINT")
    if not endpoint:
        raise ValueError("PROJECT_ENDPOINT environment variable is required")
    credential = DefaultAzureCredential()
    return AIProjectClient(endpoint=endpoint, credential=credential)


def load_instructions() -> str:
    prompts_path = Path(__file__).parent.parent / "Prompts" / "system-prompt.txt"
    if not prompts_path.exists():
        raise FileNotFoundError(f"Instructions file not found: {prompts_path}")
    return prompts_path.read_text(encoding="utf-8").strip()


def get_function_tools() -> FunctionTool:
    """Define all function tools (dispatched client-side by C# app)."""
    functions = [
        FunctionDefinition(
            name="customer_lookup",
            description="Look up customer information by customer ID or phone number",
            parameters={
                "type": "object",
                "properties": {
                    "identifier": {
                        "type": "string",
                        "description": "Customer ID or phone number in E.164 format",
                    },
                },
                "required": ["identifier"],
            },
        ),
        FunctionDefinition(
            name="order_status",
            description="Check the status of an order by order ID",
            parameters={
                "type": "object",
                "properties": {
                    "order_id": {
                        "type": "string",
                        "description": "The order ID to check status for",
                    },
                },
                "required": ["order_id"],
            },
        ),
        FunctionDefinition(
            name="check_appointment",
            description="Check existing appointments for a customer",
            parameters={
                "type": "object",
                "properties": {
                    "customer_id": {
                        "type": "string",
                        "description": "The customer ID to check appointments for",
                    },
                },
                "required": ["customer_id"],
            },
        ),
        FunctionDefinition(
            name="book_appointment",
            description="Book a new appointment for a customer",
            parameters={
                "type": "object",
                "properties": {
                    "customer_id": {
                        "type": "string",
                        "description": "The customer ID",
                    },
                    "date": {
                        "type": "string",
                        "description": "Appointment date (YYYY-MM-DD)",
                    },
                    "time": {
                        "type": "string",
                        "description": "Appointment time (HH:MM)",
                    },
                },
                "required": ["customer_id"],
            },
        ),
        FunctionDefinition(
            name="cancel_appointment",
            description="Cancel an existing appointment",
            parameters={
                "type": "object",
                "properties": {
                    "appointment_id": {
                        "type": "string",
                        "description": "The appointment ID to cancel",
                    },
                },
                "required": ["appointment_id"],
            },
        ),
        FunctionDefinition(
            name="search_knowledge_base",
            description="Search the FAQ knowledge base for information about policies, shipping, returns, etc.",
            parameters={
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "The search query",
                    },
                },
                "required": ["query"],
            },
        ),
        FunctionDefinition(
            name="transfer_call",
            description="Transfer the current call to another phone number or department",
            parameters={
                "type": "object",
                "properties": {
                    "reason": {
                        "type": "string",
                        "description": "Reason for the transfer",
                    },
                },
            },
        ),
        FunctionDefinition(
            name="end_call",
            description="End the current phone call. Call this tool IMMEDIATELY when the caller wants to hang up — do NOT speak first.",
            parameters={
                "type": "object",
                "properties": {
                    "reason": {
                        "type": "string",
                        "description": "Reason for ending the call",
                    },
                },
            },
        ),
    ]
    return FunctionTool(functions=functions)


def create_agent():
    """Create the Voice Live Standard Agent with function tools + product catalog vector store."""
    print("Creating Voice Live Standard Agent...")

    model_deployment = os.environ.get("CHAT_MODEL_DEPLOYMENT", "gpt-4.1-mini")

    instructions = load_instructions()

    project_client = get_project_client()
    openai_client = project_client.get_openai_client()

    # Step 1: Upload product catalog document
    print("  Uploading product catalog document...")
    docs_path = Path(__file__).parent / "docs" / "product-catalog.md"
    if not docs_path.exists():
        raise FileNotFoundError(f"Document not found: {docs_path}")

    with open(docs_path, "rb") as f:
        uploaded_file = openai_client.files.create(file=f, purpose="assistants")
    print(f"  FILE_ID: {uploaded_file.id}")

    # Step 2: Create vector store with the file
    print("  Creating vector store...")
    vector_store = openai_client.vector_stores.create(
        name="ProductCatalog",
        file_ids=[uploaded_file.id],
    )
    print(f"  VECTORSTORE_ID: {vector_store.id}")

    # Wait for vector store to be ready
    print("  Waiting for vector store processing...")
    while True:
        vs_status = openai_client.vector_stores.retrieve(vector_store.id)
        if vs_status.status == "completed":
            print("  Vector store ready!")
            break
        elif vs_status.status == "failed":
            raise RuntimeError(f"Vector store creation failed: {vs_status}")
        time.sleep(1)

    # Step 3: Create agent with FunctionTool (client-side) + FileSearchTool (server-side)
    print("  Creating Standard Agent...")

    function_tools = get_function_tools()
    tools = [
        function_tools,
        FileSearchTool(vector_store_ids=[vector_store.id]),
    ]

    agent_definition = PromptAgentDefinition(
        model=model_deployment,
        instructions=instructions,
        tools=tools,
    )

    agent = project_client.agents.create_version(
        agent_name=AGENT_NAME,
        definition=agent_definition,
    )

    print(f"  AGENT_ID: {agent.id}")
    print()
    print("=" * 50)
    print("Voice Live Standard Agent created successfully!")
    print(f"  Agent ID:        {agent.id}")
    print(f"  Vector Store ID: {vector_store.id}")
    print(f"  File ID:         {uploaded_file.id}")
    print(f"  Function tools:  {len(function_tools.functions)} (client-side)")
    print(f"  file_search:     product catalog (server-side)")
    print("=" * 50)
    return agent.id, vector_store.id, uploaded_file.id


def delete_agent():
    """Delete the Voice Live Standard Agent and associated resources."""
    print("Deleting Voice Live Standard Agent and resources...")

    agent_id = os.environ.get("AGENT_ID")
    vectorstore_id = os.environ.get("VECTORSTORE_ID")
    file_id = os.environ.get("FILE_ID")

    if not agent_id:
        print("No AGENT_ID found in environment. Skipping deletion.")
        return

    project_client = get_project_client()
    openai_client = project_client.get_openai_client()

    # Delete agent
    if ":" in agent_id:
        agent_name, agent_version = agent_id.rsplit(":", 1)
    else:
        agent_name = agent_id
        agent_version = "1"

    try:
        print(f"  Deleting agent: {agent_name} version {agent_version}")
        project_client.agents.delete_version(
            agent_name=agent_name, agent_version=agent_version
        )
        print("  Agent deleted.")
    except Exception as e:
        print(f"  Warning: Could not delete agent: {e}")

    # Delete vector store
    if vectorstore_id:
        try:
            print(f"  Deleting vector store: {vectorstore_id}")
            openai_client.vector_stores.delete(vectorstore_id)
            print("  Vector store deleted.")
        except Exception as e:
            print(f"  Warning: Could not delete vector store: {e}")

    # Delete file
    if file_id:
        try:
            print(f"  Deleting file: {file_id}")
            openai_client.files.delete(file_id)
            print("  File deleted.")
        except Exception as e:
            print(f"  Warning: Could not delete file: {e}")

    print()
    print("Cleanup completed.")


def main():
    if len(sys.argv) < 2:
        print("Usage: python agent_manager.py <command>")
        print("Commands:")
        print("  create  - Create agent with vector store and function tools")
        print("  delete  - Delete the agent and associated resources")
        sys.exit(1)

    command = sys.argv[1].lower()

    if command == "create":
        create_agent()
    elif command == "delete":
        delete_agent()
    else:
        print(f"Unknown command: {command}")
        sys.exit(1)


if __name__ == "__main__":
    main()
