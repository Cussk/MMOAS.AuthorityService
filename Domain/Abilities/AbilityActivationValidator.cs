namespace MMOAS.AuthorityService.Domain.Abilities;

public sealed class AbilityActivationValidator : IAbilityActivationValidator
{
    public AbilityActivationDecision Evaluate(AbilityActivationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.SessionExists)
        {
            return Reject(
                context,
                "activation.session-not-found",
                "Cannot activate an ability for an unknown session.");
        }

        if (!context.HelloCompleted)
        {
            return Reject(
                context,
                "activation.hello-required",
                "A session must complete hello before activating an ability.");
        }

        if (string.IsNullOrWhiteSpace(context.EntityId))
        {
            return Reject(
                context,
                "activation.entity-required",
                "A session must register an entity before activating an ability.");
        }

        if (string.IsNullOrWhiteSpace(context.AbilityId))
        {
            return Reject(
                context,
                "activation.invalid-ability",
                "Ability id must be non-empty.");
        }

        return new AbilityActivationDecision(
            true,
            context.SessionId,
            context.EntityId,
            context.AbilityId,
            null,
            null);
    }

    private static AbilityActivationDecision Reject(
        AbilityActivationContext context,
        string code,
        string message)
    {
        return new AbilityActivationDecision(
            false,
            context.SessionId,
            context.EntityId,
            context.AbilityId,
            code,
            message);
    }
}
