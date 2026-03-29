namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record EntityRegisteredMessage(string SessionId, string EntityId, DateTimeOffset RegisteredAtUtc);
