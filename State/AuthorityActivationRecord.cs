namespace MMOAS.AuthorityService.State;

public sealed record AuthorityActivationRecord(
    string ActivationInstanceId,
    string SessionId,
    string EntityId,
    string AbilityId,
    DateTimeOffset CreatedAtUtc);
