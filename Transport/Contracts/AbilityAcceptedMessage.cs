namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record AbilityAcceptedMessage(
    string SessionId,
    string EntityId,
    string AbilityId,
    string ActivationInstanceId);
