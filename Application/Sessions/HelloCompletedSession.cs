namespace MMOAS.AuthorityService.Application.Sessions;

public sealed record HelloCompletedSession(string SessionId, DateTimeOffset ConnectedAtUtc, bool HelloCompleted);
