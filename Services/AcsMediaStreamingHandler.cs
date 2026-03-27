using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ACSVoiceAgent.Services;

public class AcsMediaStreamingHandler
{
    private readonly WebSocket _webSocket;
    private readonly ILogger<AcsMediaStreamingHandler> _logger;

    public AcsMediaStreamingHandler(WebSocket webSocket, ILogger<AcsMediaStreamingHandler> logger)
    {
        _webSocket = webSocket;
        _logger = logger;
    }

    public async Task SendMessageAsync(string message)
    {
        if (_webSocket.State != WebSocketState.Open)
        {
            _logger.LogWarning("Attempted to send message on closed WebSocket (state: {State})", _webSocket.State);
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send message on ACS WebSocket");
        }
    }

    public async Task<(string? message, bool endOfStream)> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("ACS WebSocket closed");
                return (null, true);
            }

            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        } while (!result.EndOfMessage);

        return (messageBuilder.ToString(), false);
    }
}

public static class OutStreamingData
{
    public static string GetAudioDataForOutbound(byte[] audioData) =>
        JsonSerializer.Serialize(new
        {
            kind = "AudioData",
            audioData = new { data = Convert.ToBase64String(audioData) }
        });

    public static string GetStopAudioForOutbound() =>
        JsonSerializer.Serialize(new { kind = "StopAudio" });
}
