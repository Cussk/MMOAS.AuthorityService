using MMOAS.AuthorityService.Domain.Abilities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Application.Abilities;

public sealed class AbilityActivationService : IAbilityActivationService
{
    private readonly IAuthoritySessionStore _sessionStore;
    private readonly IAbilityActivationValidator _validator;

    public AbilityActivationService(
        IAuthoritySessionStore sessionStore,
        IAbilityActivationValidator validator)
    {
        _sessionStore = sessionStore;
        _validator = validator;
    }

    public ValueTask<AbilityActivationResult> ActivateAsync(
        string sessionId,
        string? abilityId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = _sessionStore.Get(sessionId);
        var normalizedAbilityId = abilityId?.Trim() ?? string.Empty;

        var context = new AbilityActivationContext(
            sessionId,
            session is not null,
            session?.HelloCompleted ?? false,
            session?.RegisteredEntityId,
            normalizedAbilityId);

        var decision = _validator.Evaluate(context);
        var result = new AbilityActivationResult(
            decision.Accepted,
            decision.SessionId,
            decision.EntityId,
            decision.AbilityId,
            decision.Code,
            decision.Message);

        return ValueTask.FromResult(result);
    }
}
