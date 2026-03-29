using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MMOAS.AuthorityService.Application.Abilities;
using MMOAS.AuthorityService.Application.Sessions;
using MMOAS.AuthorityService.Transport.Contracts;

namespace MMOAS.AuthorityService.Transport;

public sealed class AuthorityWebSocketSessionHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeProvider _timeProvider;
    private readonly IAbilityActivationService _abilityActivationService;
    private readonly IAuthoritySessionService _sessionService;
    private readonly ILogger<AuthorityWebSocketSessionHandler> _logger;

    public AuthorityWebSocketSessionHandler(
        TimeProvider timeProvider,
        IAbilityActivationService abilityActivationService,
        IAuthoritySessionService sessionService,
        ILogger<AuthorityWebSocketSessionHandler> logger)
    {
        _timeProvider = timeProvider;
        _abilityActivationService = abilityActivationService;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            return Results.BadRequest(new { Error = "Expected a WebSocket upgrade request." });
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var sessionId = context.TraceIdentifier;

        await _sessionService.CreateSessionAsync(sessionId, cancellationToken);
        _logger.LogInformation("Accepted WebSocket session {SessionId}", sessionId);

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                SocketMessage received;

                try
                {
                    received = await ReceiveMessageAsync(socket, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (received.IsClose)
                {
                    break;
                }

                if (!received.IsText)
                {
                    await SendErrorAsync(
                        socket,
                        null,
                        "transport.invalid-message-type",
                        "Expected a text WebSocket message.",
                        cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(received.MessageText))
                {
                    await SendErrorAsync(
                        socket,
                        null,
                        "transport.invalid-payload",
                        "Expected a non-empty transport message.",
                        cancellationToken);
                    continue;
                }

                await ProcessMessageAsync(socket, sessionId, received.MessageText, cancellationToken);
            }
        }
        finally
        {
            await _sessionService.RemoveSessionAsync(sessionId);

            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Phase 03 transport session complete.",
                    CancellationToken.None);
            }

            _logger.LogInformation("Closed WebSocket session {SessionId}", sessionId);
        }

        return Results.Empty;
    }

    private async Task ProcessMessageAsync(
        WebSocket socket,
        string sessionId,
        string messageText,
        CancellationToken cancellationToken)
    {
        AuthorityInboundMessageEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<AuthorityInboundMessageEnvelope>(messageText, SerializerOptions);
        }
        catch (JsonException)
        {
            await SendErrorAsync(
                socket,
                null,
                "transport.invalid-json",
                "The transport message was not valid JSON.",
                cancellationToken);
            return;
        }

        if (envelope is null || string.IsNullOrWhiteSpace(envelope.MessageType))
        {
            await SendErrorAsync(
                socket,
                null,
                "transport.invalid-envelope",
                "The transport message envelope is missing required fields.",
                cancellationToken);
            return;
        }

        if (envelope.Version != AuthorityTransportProtocol.Version)
        {
            await SendErrorAsync(
                socket,
                envelope.RequestId,
                "transport.unsupported-version",
                $"Unsupported protocol version '{envelope.Version}'.",
                cancellationToken);
            return;
        }

        try
        {
            switch (envelope.MessageType)
            {
                case AuthorityTransportProtocol.HelloMessageType:
                    await HandleHelloAsync(socket, sessionId, envelope, cancellationToken);
                    break;

                case AuthorityTransportProtocol.RegisterEntityMessageType:
                    await HandleRegisterEntityAsync(socket, sessionId, envelope, cancellationToken);
                    break;

                case AuthorityTransportProtocol.ActivateAbilityMessageType:
                    await HandleActivateAbilityAsync(socket, sessionId, envelope, cancellationToken);
                    break;

                default:
                    await SendErrorAsync(
                        socket,
                        envelope.RequestId,
                        "transport.unsupported-message",
                        $"Unsupported message type '{envelope.MessageType}'.",
                        cancellationToken);
                    break;
            }
        }
        catch (AuthoritySessionException exception)
        {
            await SendErrorAsync(
                socket,
                envelope.RequestId,
                exception.Code,
                exception.Message,
                cancellationToken);
        }
    }

    private async Task HandleHelloAsync(
        WebSocket socket,
        string sessionId,
        AuthorityInboundMessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!TryDeserializePayload<HelloCommand>(envelope.Payload, out _))
        {
            await SendErrorAsync(
                socket,
                envelope.RequestId,
                "transport.invalid-payload",
                "The hello command payload is invalid.",
                cancellationToken);
            return;
        }

        var helloCompleted = await _sessionService.CompleteHelloAsync(sessionId, cancellationToken);
        var readyMessage = new WebSocketReadyMessage(
            helloCompleted.SessionId,
            helloCompleted.ConnectedAtUtc,
            helloCompleted.HelloCompleted);

        await SendEnvelopeAsync(
            socket,
            AuthorityTransportProtocol.ReadyMessageType,
            envelope.RequestId,
            readyMessage,
            cancellationToken);
    }

    private async Task HandleRegisterEntityAsync(
        WebSocket socket,
        string sessionId,
        AuthorityInboundMessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!TryDeserializePayload<RegisterEntityCommand>(envelope.Payload, out _))
        {
            await SendErrorAsync(
                socket,
                envelope.RequestId,
                "transport.invalid-payload",
                "The register-entity command payload is invalid.",
                cancellationToken);
            return;
        }

        var registeredEntity = await _sessionService.RegisterEntityAsync(sessionId, cancellationToken);
        var entityRegisteredMessage = new EntityRegisteredMessage(
            registeredEntity.SessionId,
            registeredEntity.EntityId,
            registeredEntity.RegisteredAtUtc);

        await SendEnvelopeAsync(
            socket,
            AuthorityTransportProtocol.EntityRegisteredMessageType,
            envelope.RequestId,
            entityRegisteredMessage,
            cancellationToken);
    }

    private async Task HandleActivateAbilityAsync(
        WebSocket socket,
        string sessionId,
        AuthorityInboundMessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!TryDeserializePayload<ActivateAbilityCommand>(envelope.Payload, out var command))
        {
            await SendErrorAsync(
                socket,
                envelope.RequestId,
                "transport.invalid-payload",
                "The activate-ability command payload is invalid.",
                cancellationToken);
            return;
        }

        var activationResult = await _abilityActivationService.ActivateAsync(
            sessionId,
            command!.AbilityId,
            cancellationToken);

        if (activationResult.Accepted)
        {
            var acceptedMessage = new AbilityAcceptedMessage(
                activationResult.SessionId,
                activationResult.EntityId!,
                activationResult.AbilityId,
                activationResult.ActivationInstanceId!);

            await SendEnvelopeAsync(
                socket,
                AuthorityTransportProtocol.AbilityAcceptedMessageType,
                envelope.RequestId,
                acceptedMessage,
                cancellationToken);

            return;
        }

        var rejectedAbilityId = string.IsNullOrWhiteSpace(activationResult.AbilityId)
            ? null
            : activationResult.AbilityId;
        var rejectedMessage = new AbilityRejectedMessage(
            activationResult.Code!,
            activationResult.Message!,
            rejectedAbilityId);

        await SendEnvelopeAsync(
            socket,
            AuthorityTransportProtocol.AbilityRejectedMessageType,
            envelope.RequestId,
            rejectedMessage,
            cancellationToken);
    }

    private async Task SendErrorAsync(
        WebSocket socket,
        string? requestId,
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        await SendEnvelopeAsync(
            socket,
            AuthorityTransportProtocol.ErrorMessageType,
            requestId,
            new TransportErrorMessage(code, message),
            cancellationToken);
    }

    private async Task SendEnvelopeAsync(
        WebSocket socket,
        string messageType,
        string? requestId,
        object payload,
        CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var envelope = new AuthorityOutboundMessageEnvelope(
            messageType,
            AuthorityTransportProtocol.Version,
            _timeProvider.GetUtcNow(),
            requestId,
            payload);

        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static bool TryDeserializePayload<T>(JsonElement? payload, out T? message)
    {
        message = default;

        if (!payload.HasValue || payload.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        try
        {
            message = payload.Value.Deserialize<T>(SerializerOptions);
            return message is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task<SocketMessage> ReceiveMessageAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);

        try
        {
            using var messageBuffer = new MemoryStream();
            WebSocketMessageType? messageType = null;

            while (true)
            {
                var received = await socket.ReceiveAsync(buffer, cancellationToken);

                if (received.MessageType == WebSocketMessageType.Close)
                {
                    return SocketMessage.Close();
                }

                messageType ??= received.MessageType;

                if (received.Count > 0)
                {
                    messageBuffer.Write(buffer, 0, received.Count);
                }

                if (received.EndOfMessage)
                {
                    break;
                }
            }

            if (messageType != WebSocketMessageType.Text)
            {
                return SocketMessage.NonText();
            }

            var messageText = Encoding.UTF8.GetString(messageBuffer.GetBuffer().AsSpan(0, (int)messageBuffer.Length));
            return SocketMessage.Text(messageText);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private sealed record SocketMessage(bool IsClose, bool IsText, string? MessageText)
    {
        public static SocketMessage Close() => new(true, false, null);

        public static SocketMessage NonText() => new(false, false, null);

        public static SocketMessage Text(string messageText) => new(false, true, messageText);
    }
}
