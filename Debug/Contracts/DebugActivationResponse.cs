namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugActivationResponse(
    string ActivationInstanceId,
    string SessionId,
    string EntityId,
    string AbilityId,
    string Phase,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CommitDueAtUtc,
    DateTimeOffset? CommittedAtUtc,
    string? InterruptionCode,
    DateTimeOffset? InterruptedAtUtc);
