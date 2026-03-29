using System.Collections.Concurrent;

namespace MMOAS.AuthorityService.State;

public sealed class InMemoryAuthoritySessionStore : IAuthoritySessionStore
{
    private readonly ConcurrentDictionary<string, AuthoritySessionRecord> _sessions = new(StringComparer.Ordinal);

    public AuthoritySessionRecord CreateOrGet(string sessionId, DateTimeOffset connectedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return _sessions.GetOrAdd(sessionId, id => new AuthoritySessionRecord(id, connectedAtUtc, false, null));
    }

    public AuthoritySessionRecord? Get(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public AuthoritySessionRecord? MarkHelloCompleted(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        while (true)
        {
            if (!_sessions.TryGetValue(sessionId, out var existingSession))
            {
                return null;
            }

            if (existingSession.HelloCompleted)
            {
                return existingSession;
            }

            var updatedSession = existingSession with { HelloCompleted = true };

            // Compare-and-swap keeps per-session updates consistent without a coarse lock across sockets.
            if (_sessions.TryUpdate(sessionId, updatedSession, existingSession))
            {
                return updatedSession;
            }
        }
    }

    public AuthoritySessionRecord? AssignRegisteredEntity(string sessionId, string entityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        while (true)
        {
            if (!_sessions.TryGetValue(sessionId, out var existingSession))
            {
                return null;
            }

            var updatedSession = existingSession with { RegisteredEntityId = entityId };

            if (_sessions.TryUpdate(sessionId, updatedSession, existingSession))
            {
                return updatedSession;
            }
        }
    }

    public bool TryRemove(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        return _sessions.TryRemove(sessionId, out _);
    }

    public AuthoritySessionSnapshot GetSnapshot()
    {
        var snapshot = _sessions.Values
            .OrderBy(session => session.ConnectedAtUtc)
            .ThenBy(session => session.SessionId, StringComparer.Ordinal)
            .ToArray();

        return new AuthoritySessionSnapshot(snapshot);
    }
}
