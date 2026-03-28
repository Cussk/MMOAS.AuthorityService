namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record RegisterEntityHttpResponse(string EntityId, DateTimeOffset RegisteredAtUtc);
