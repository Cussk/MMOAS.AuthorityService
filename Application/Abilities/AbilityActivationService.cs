using MMOAS.AuthorityService.Domain.Abilities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Application.Abilities;

public sealed class AbilityActivationService : IAbilityActivationService
{
    private readonly IAuthorityActivationStore _activationStore;
    private readonly IAuthoritySessionStore _sessionStore;
    private readonly TimeProvider _timeProvider;
    private readonly IAbilityActivationValidator _validator;

    public AbilityActivationService(
        IAuthorityActivationStore activationStore,
        IAuthoritySessionStore sessionStore,
        IAbilityActivationValidator validator,
        TimeProvider timeProvider)
    {
        _activationStore = activationStore;
        _sessionStore = sessionStore;
        _validator = validator;
        _timeProvider = timeProvider;
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
        var activationInstanceId = decision.Accepted
            ? CreateActivationInstance(sessionId, decision.EntityId!, decision.AbilityId)
            : null;
        var result = new AbilityActivationResult(
            decision.Accepted,
            decision.SessionId,
            decision.EntityId,
            decision.AbilityId,
            activationInstanceId,
            decision.Code,
            decision.Message);

        return ValueTask.FromResult(result);
    }

    private string CreateActivationInstance(string sessionId, string entityId, string abilityId)
    {
        while (true)
        {
            // Activation instances are backend-owned so later lifecycle/timing phases can correlate against
            // a server-generated runtime identity rather than any client-local prediction token.
            var activation = new AuthorityActivationRecord(
                Guid.NewGuid().ToString("N"),
                sessionId,
                entityId,
                abilityId,
                _timeProvider.GetUtcNow());

            if (_activationStore.TryAdd(activation))
            {
                return activation.ActivationInstanceId;
            }
        }
    }
}
