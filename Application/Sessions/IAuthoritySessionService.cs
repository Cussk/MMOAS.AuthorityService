namespace MMOAS.AuthorityService.Application.Sessions;

public interface IAuthoritySessionService
{
    ValueTask CreateSessionAsync(string sessionId, CancellationToken cancellationToken);

    ValueTask<HelloCompletedSession> CompleteHelloAsync(string sessionId, CancellationToken cancellationToken);

    ValueTask<RegisteredSessionEntity> RegisterEntityAsync(string sessionId, CancellationToken cancellationToken);

    ValueTask RemoveSessionAsync(string sessionId);
}
