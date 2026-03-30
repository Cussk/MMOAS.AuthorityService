namespace MMOAS.AuthorityService.Application.Abilities;

public interface IAbilityActivationService
{
    ValueTask<AbilityActivationResult> ActivateAsync(
        string sessionId,
        string? abilityId,
        CancellationToken cancellationToken);

    ValueTask<AbilityInterruptionResult> InterruptAsync(
        string sessionId,
        string? activationInstanceId,
        string? interruptionCode,
        CancellationToken cancellationToken);
}
