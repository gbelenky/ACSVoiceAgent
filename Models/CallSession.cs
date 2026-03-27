using System.Net.WebSockets;
using Azure.AI.VoiceLive;

namespace ACSVoiceAgent.Models;

public enum CallStatus
{
    Active,
    Transferring,
    Completed
}

public class ToolInvocation
{
    public string ToolName { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class TranscriptEntry
{
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class CallSession
{
    public string CallConnectionId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string CallerId { get; set; } = string.Empty;
    public WebSocket? AcsWebSocket { get; set; }
    public VoiceLiveSession? VoiceLiveSession { get; set; }
    public CancellationTokenSource? Cts { get; set; }
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public List<ToolInvocation> ToolInvocations { get; set; } = new();
    public List<TranscriptEntry> Transcript { get; set; } = new();
    public CallStatus Status { get; set; } = CallStatus.Active;
}
