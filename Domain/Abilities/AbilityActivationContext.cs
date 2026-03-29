namespace MMOAS.AuthorityService.Domain.Abilities;

public sealed record AbilityActivationContext(
    string SessionId,
    bool SessionExists,
    bool HelloCompleted,
    string? EntityId,
    string AbilityId);
