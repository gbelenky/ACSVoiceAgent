using System.Collections.Concurrent;
using System.Threading.Channels;
using ACSVoiceAgent.Models;

namespace ACSVoiceAgent.Services;

public class CallSessionManager
{
    private readonly ConcurrentDictionary<string, CallSession> _sessions = new();
    private readonly Channel<CallSession> _newSessions = Channel.CreateUnbounded<CallSession>();
    private readonly ILogger<CallSessionManager> _logger;

    public CallSessionManager(ILogger<CallSessionManager> logger)
    {
        _logger = logger;
    }

    public CallSession CreateSession(string callConnectionId, string correlationId, string callerId)
    {
        var session = new CallSession
        {
            CallConnectionId = callConnectionId,
            CorrelationId = correlationId,
            CallerId = callerId,
            Cts = new CancellationTokenSource()
        };

        if (_sessions.TryAdd(correlationId, session))
        {
            _logger.LogInformation("Created call session for correlation {CorrelationId}, connection {CallConnectionId}",
                correlationId, callConnectionId);
            _newSessions.Writer.TryWrite(session);
            return session;
        }

        _logger.LogWarning("Session already exists for correlation {CorrelationId}", correlationId);
        return _sessions[correlationId];
    }

    /// <summary>
    /// Waits for the next session to be created (from CallConnected callback).
    /// Returns immediately if an unbound session already exists.
    /// </summary>
    public async Task<CallSession?> WaitForSessionAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        // Check existing sessions first
        var session = GetLatestUnboundSession();
        if (session != null) return session;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            _logger.LogInformation("Waiting up to {Timeout}s for CallConnected session...", timeout.TotalSeconds);
            session = await _newSessions.Reader.ReadAsync(cts.Token);
            _logger.LogInformation("Session received: connection {CallConnectionId}", session.CallConnectionId);
            return session;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timed out waiting {Timeout}s for a call session", timeout.TotalSeconds);
            return null;
        }
    }

    public void RemoveSession(string correlationId)
    {
        if (_sessions.TryRemove(correlationId, out var session))
        {
            _logger.LogInformation("Removing call session for correlation {CorrelationId}", correlationId);
            session.Status = CallStatus.Completed;
            session.Cts?.Cancel();
            session.Cts?.Dispose();

            if (session.VoiceLiveSession != null)
            {
                try
                {
                    session.VoiceLiveSession.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing VoiceLive session for {CorrelationId}", correlationId);
                }
            }

            _logger.LogInformation(
                "Call session ended. Duration: {Duration}, Tools called: {ToolCount}",
                DateTimeOffset.UtcNow - session.StartTime,
                session.ToolInvocations.Count);
        }
    }

    public int ActiveSessionCount => _sessions.Count;

    public CallSession? GetLatestUnboundSession()
    {
        return _sessions.Values
            .Where(s => s.Status == CallStatus.Active && s.VoiceLiveSession == null)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefault();
    }
}
