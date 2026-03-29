namespace MMOAS.AuthorityService.Domain.Abilities;

public sealed record AbilityActivationDecision(
    bool Accepted,
    string SessionId,
    string? EntityId,
    string AbilityId,
    string? Code,
    string? Message);
