using System.Collections.Concurrent;
using ACSVoiceAgent.Models;

namespace ACSVoiceAgent.Services;

public class CallSessionManager
{
    // Key: contextId (the GUID from AnswerCall, shared between callback URL and WS URL)
    private readonly ConcurrentDictionary<string, CallSession> _sessions = new();
    // Key: contextId → signaled when CallConnected fires
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CallSession>> _pendingSessions = new();
    private readonly ILogger<CallSessionManager> _logger;

    public CallSessionManager(ILogger<CallSessionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a pending session when the call is answered.
    /// Must be called BEFORE AnswerCallAsync so the WS handler can find it.
    /// </summary>
    public void RegisterPendingSession(string contextId)
    {
        _pendingSessions.TryAdd(contextId, new TaskCompletionSource<CallSession>(TaskCreationOptions.RunContinuationsAsynchronously));
        _logger.LogInformation("Registered pending session for context {ContextId}", contextId);
    }

    /// <summary>
    /// Creates the session when CallConnected fires and signals the waiting WS handler.
    /// </summary>
    public CallSession CreateSession(string contextId, string callConnectionId, string correlationId, string callerId)
    {
        var session = new CallSession
        {
            CallConnectionId = callConnectionId,
            CorrelationId = correlationId,
            CallerId = callerId,
            Cts = new CancellationTokenSource()
        };

        _sessions.TryAdd(contextId, session);
        _logger.LogInformation("Created call session for context {ContextId}, connection {CallConnectionId}",
            contextId, callConnectionId);

        // Signal the WS handler waiting on this contextId
        if (_pendingSessions.TryRemove(contextId, out var tcs))
        {
            tcs.TrySetResult(session);
        }

        return session;
    }

    /// <summary>
    /// Waits for CallConnected to create the session for a specific call context.
    /// Returns immediately if the session already exists (CallConnected arrived before WS).
    /// </summary>
    public async Task<CallSession?> WaitForSessionAsync(string contextId, TimeSpan timeout, CancellationToken ct = default)
    {
        // If CallConnected already fired before WS connected, return the session immediately
        if (_sessions.TryGetValue(contextId, out var existing))
            return existing;

        if (!_pendingSessions.TryGetValue(contextId, out var tcs))
        {
            _logger.LogWarning("No pending session registered for context {ContextId}", contextId);
            return null;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            _logger.LogInformation("Waiting up to {Timeout}s for CallConnected (context {ContextId})...",
                timeout.TotalSeconds, contextId);
            return await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timed out waiting {Timeout}s for session (context {ContextId})",
                timeout.TotalSeconds, contextId);
            _pendingSessions.TryRemove(contextId, out _);
            return null;
        }
    }

    public void RemoveSession(string correlationId)
    {
        // Find by correlationId (CallDisconnected only provides correlationId)
        var entry = _sessions.FirstOrDefault(kvp => kvp.Value.CorrelationId == correlationId);
        if (entry.Key != null && _sessions.TryRemove(entry.Key, out var session))
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
}
