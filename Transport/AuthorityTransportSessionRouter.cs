using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using MMOAS.AuthorityService.Transport.Contracts;

namespace MMOAS.AuthorityService.Transport;

public sealed class AuthorityTransportSessionRouter : IAuthoritySessionNotifier
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, SessionConnection> _connections = new(StringComparer.Ordinal);

    public AuthorityTransportSessionRouter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void RegisterSession(string sessionId, WebSocket socket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(socket);

        _connections[sessionId] = new SessionConnection(socket);
    }

    public async ValueTask UnregisterSessionAsync(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (_connections.TryRemove(sessionId, out var connection))
        {
            await connection.StopAcceptingSendsAsync();
        }
    }

    public ValueTask<bool> SendAsync(
        string sessionId,
        string messageType,
        string? requestId,
        object payload,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(payload);

        if (!_connections.TryGetValue(sessionId, out var connection))
        {
            return ValueTask.FromResult(false);
        }

        var envelope = new AuthorityOutboundMessageEnvelope(
            messageType,
            AuthorityTransportProtocol.Version,
            _timeProvider.GetUtcNow(),
            requestId,
            payload);

        return connection.SendAsync(envelope, cancellationToken);
    }

    public ValueTask<bool> NotifyAbilityCommittedAsync(
        AbilityCommittedNotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return SendAsync(
            notification.SessionId,
            AuthorityTransportProtocol.AbilityCommittedMessageType,
            null,
            new AbilityCommittedMessage(
                notification.SessionId,
                notification.EntityId,
                notification.AbilityId,
                notification.ActivationInstanceId,
                notification.CommittedAtUtc),
            cancellationToken);
    }

    private sealed class SessionConnection
    {
        private readonly WebSocket _socket;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private int _acceptingSends = 1;

        public SessionConnection(WebSocket socket)
        {
            _socket = socket;
        }

        public async ValueTask<bool> SendAsync(AuthorityOutboundMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _acceptingSends) == 0)
            {
                return false;
            }

            await _sendLock.WaitAsync(cancellationToken);

            try
            {
                if (Volatile.Read(ref _acceptingSends) == 0 || _socket.State != WebSocketState.Open)
                {
                    return false;
                }

                var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions);
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);

                return true;
            }
            catch (WebSocketException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async ValueTask StopAcceptingSendsAsync()
        {
            Interlocked.Exchange(ref _acceptingSends, 0);
            await _sendLock.WaitAsync();
            _sendLock.Release();
        }
    }
}
