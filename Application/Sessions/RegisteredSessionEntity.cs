namespace MMOAS.AuthorityService.Application.Sessions;

public sealed record RegisteredSessionEntity(string SessionId, string EntityId, DateTimeOffset RegisteredAtUtc);
