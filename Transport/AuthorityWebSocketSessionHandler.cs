using System.Net.WebSockets;
using System.Text.Json;
using MMOAS.AuthorityService.Transport.Contracts;

namespace MMOAS.AuthorityService.Transport;

public sealed class AuthorityWebSocketSessionHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthorityWebSocketSessionHandler> _logger;

    public AuthorityWebSocketSessionHandler(TimeProvider timeProvider, ILogger<AuthorityWebSocketSessionHandler> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            return Results.BadRequest(new { Error = "Expected a WebSocket upgrade request." });
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = context.TraceIdentifier;

        _logger.LogInformation("Accepted WebSocket session {ConnectionId}", connectionId);

        var readyMessage = new WebSocketReadyMessage("transport.ready", _timeProvider.GetUtcNow());
        var payload = JsonSerializer.SerializeToUtf8Bytes(readyMessage, SerializerOptions);

        await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);

        var buffer = new byte[1024];

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult received;

            try
            {
                received = await socket.ReceiveAsync(buffer, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (received.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
        }

        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Phase 00 transport stub complete.",
                cancellationToken);
        }

        _logger.LogInformation("Closed WebSocket session {ConnectionId}", connectionId);

        return Results.Empty;
    }
}
