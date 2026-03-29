namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugSessionResponse(
    string SessionId,
    DateTimeOffset ConnectedAtUtc,
    bool HelloCompleted,
    string? RegisteredEntityId);
