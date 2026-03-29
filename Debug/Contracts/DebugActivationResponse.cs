namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugActivationResponse(
    string ActivationInstanceId,
    string SessionId,
    string EntityId,
    string AbilityId,
    DateTimeOffset CreatedAtUtc);
