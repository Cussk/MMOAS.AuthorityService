namespace MMOAS.AuthorityService.Application.Abilities;

public sealed record AbilityInterruptionResult(
    bool Interrupted,
    string SessionId,
    string? EntityId,
    string? AbilityId,
    string? ActivationInstanceId,
    string? InterruptionCode,
    DateTimeOffset? InterruptedAtUtc,
    string? Code,
    string? Message);
