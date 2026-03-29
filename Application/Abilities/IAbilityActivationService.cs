namespace MMOAS.AuthorityService.Application.Abilities;

public interface IAbilityActivationService
{
    ValueTask<AbilityActivationResult> ActivateAsync(
        string sessionId,
        string? abilityId,
        CancellationToken cancellationToken);
}
