using MMOAS.AuthorityService.Application.Entities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Application.Sessions;

public sealed class AuthoritySessionService : IAuthoritySessionService
{
    private readonly IAuthoritySessionStore _sessionStore;
    private readonly IEntityRegistrationService _entityRegistrationService;
    private readonly TimeProvider _timeProvider;

    public AuthoritySessionService(
        IAuthoritySessionStore sessionStore,
        IEntityRegistrationService entityRegistrationService,
        TimeProvider timeProvider)
    {
        _sessionStore = sessionStore;
        _entityRegistrationService = entityRegistrationService;
        _timeProvider = timeProvider;
    }

    public ValueTask CreateSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _sessionStore.CreateOrGet(sessionId, _timeProvider.GetUtcNow());

        return ValueTask.CompletedTask;
    }

    public ValueTask<HelloCompletedSession> CompleteHelloAsync(string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _sessionStore.MarkHelloCompleted(sessionId)
            ?? throw new AuthoritySessionException(
                "session.not-found",
                "Cannot complete hello for an unknown session.");

        return ValueTask.FromResult(new HelloCompletedSession(
            session.SessionId,
            session.ConnectedAtUtc,
            session.HelloCompleted));
    }

    public async ValueTask<RegisteredSessionEntity> RegisterEntityAsync(string sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _sessionStore.Get(sessionId)
            ?? throw new AuthoritySessionException(
                "session.not-found",
                "Cannot register an entity for an unknown session.");

        if (!session.HelloCompleted)
        {
            throw new AuthoritySessionException(
                "session.hello-required",
                "A session must complete hello before registering an entity.");
        }

        var registeredEntity = await _entityRegistrationService.RegisterAsync(cancellationToken);
        var updatedSession = _sessionStore.AssignRegisteredEntity(sessionId, registeredEntity.EntityId)
            ?? throw new AuthoritySessionException(
                "session.not-found",
                "The session closed before the entity registration could be associated.");

        return new RegisteredSessionEntity(
            updatedSession.SessionId,
            registeredEntity.EntityId,
            registeredEntity.RegisteredAtUtc);
    }

    public ValueTask RemoveSessionAsync(string sessionId)
    {
        _sessionStore.TryRemove(sessionId);

        return ValueTask.CompletedTask;
    }
}
