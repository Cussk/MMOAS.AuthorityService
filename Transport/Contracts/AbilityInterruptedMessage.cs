namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record AbilityInterruptedMessage(
    string SessionId,
    string EntityId,
    string AbilityId,
    string ActivationInstanceId,
    string InterruptionCode,
    DateTimeOffset InterruptedAtUtc);
