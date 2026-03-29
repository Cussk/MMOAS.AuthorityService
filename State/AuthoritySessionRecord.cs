namespace MMOAS.AuthorityService.State;

public sealed record AuthoritySessionRecord(
    string SessionId,
    DateTimeOffset ConnectedAtUtc,
    bool HelloCompleted,
    string? RegisteredEntityId);
