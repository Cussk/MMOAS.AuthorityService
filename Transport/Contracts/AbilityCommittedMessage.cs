namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record AbilityCommittedMessage(
    string SessionId,
    string EntityId,
    string AbilityId,
    string ActivationInstanceId,
    DateTimeOffset CommittedAtUtc);
