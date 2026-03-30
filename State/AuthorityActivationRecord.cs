namespace MMOAS.AuthorityService.State;

public sealed record AuthorityActivationRecord(
    string ActivationInstanceId,
    string SessionId,
    string EntityId,
    string AbilityId,
    AuthorityActivationPhase Phase,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset CommitDueAtUtc,
    DateTimeOffset? CommittedAtUtc);
