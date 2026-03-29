namespace MMOAS.AuthorityService.Application.Abilities;

public sealed record AbilityActivationResult(
    bool Accepted,
    string SessionId,
    string? EntityId,
    string AbilityId,
    string? Code,
    string? Message);
