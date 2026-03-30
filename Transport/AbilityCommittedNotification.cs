namespace MMOAS.AuthorityService.Transport;

public sealed record AbilityCommittedNotification(
    string SessionId,
    string EntityId,
    string AbilityId,
    string ActivationInstanceId,
    DateTimeOffset CommittedAtUtc);
