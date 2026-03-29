namespace MMOAS.AuthorityService.Domain.Abilities;

public interface IAbilityActivationValidator
{
    AbilityActivationDecision Evaluate(AbilityActivationContext context);
}
