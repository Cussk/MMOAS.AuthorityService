using MMOAS.AuthorityService.Domain.Abilities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Application.Abilities;

public sealed class AbilityActivationService : IAbilityActivationService
{
    private static readonly TimeSpan StartupDuration = TimeSpan.FromMilliseconds(500);
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

    public ValueTask<AbilityInterruptionResult> InterruptAsync(
        string sessionId,
        string? activationInstanceId,
        string? interruptionCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedActivationInstanceId = activationInstanceId?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedActivationInstanceId))
        {
            return ValueTask.FromResult(RejectInterruption(
                sessionId,
                null,
                null,
                null,
                null,
                "activation.interrupt.invalid-activation-instance",
                "An interruption requires a non-empty activation instance id."));
        }

        var normalizedInterruptionCode = interruptionCode?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedInterruptionCode))
        {
            return ValueTask.FromResult(RejectInterruption(
                sessionId,
                null,
                null,
                normalizedActivationInstanceId,
                null,
                "activation.interrupt.invalid-code",
                "An interruption requires a non-empty interruption code."));
        }

        var existingActivation = _activationStore.Get(normalizedActivationInstanceId);

        if (existingActivation is null)
        {
            return ValueTask.FromResult(RejectInterruption(
                sessionId,
                null,
                null,
                normalizedActivationInstanceId,
                normalizedInterruptionCode,
                "activation.interrupt.not-found",
                $"Activation '{normalizedActivationInstanceId}' was not found."));
        }

        if (!string.Equals(existingActivation.SessionId, sessionId, StringComparison.Ordinal))
        {
            return ValueTask.FromResult(RejectInterruption(
                sessionId,
                null,
                null,
                normalizedActivationInstanceId,
                normalizedInterruptionCode,
                "activation.interrupt.session-mismatch",
                "An activation may only be interrupted by its owning session."));
        }

        if (existingActivation.Phase != AuthorityActivationPhase.Accepted)
        {
            return ValueTask.FromResult(RejectInterruption(
                existingActivation.SessionId,
                existingActivation.EntityId,
                existingActivation.AbilityId,
                existingActivation.ActivationInstanceId,
                normalizedInterruptionCode,
                "activation.interrupt.not-interruptible",
                $"Activation '{existingActivation.ActivationInstanceId}' is already {existingActivation.Phase}."));
        }

        var interruptedActivation = _activationStore.TryMarkInterrupted(
            normalizedActivationInstanceId,
            normalizedInterruptionCode,
            _timeProvider.GetUtcNow());

        if (interruptedActivation is not null)
        {
            return ValueTask.FromResult(new AbilityInterruptionResult(
                true,
                interruptedActivation.SessionId,
                interruptedActivation.EntityId,
                interruptedActivation.AbilityId,
                interruptedActivation.ActivationInstanceId,
                interruptedActivation.InterruptionCode,
                interruptedActivation.InterruptedAtUtc,
                null,
                null));
        }

        var currentActivation = _activationStore.Get(normalizedActivationInstanceId);
        var currentPhase = currentActivation?.Phase.ToString() ?? "Unavailable";

        return ValueTask.FromResult(RejectInterruption(
            sessionId,
            currentActivation?.EntityId,
            currentActivation?.AbilityId,
            normalizedActivationInstanceId,
            normalizedInterruptionCode,
            "activation.interrupt.not-interruptible",
            $"Activation '{normalizedActivationInstanceId}' is already {currentPhase}."));
    }

    private string CreateActivationInstance(string sessionId, string entityId, string abilityId)
    {
        while (true)
        {
            var createdAtUtc = _timeProvider.GetUtcNow();

            // Activation instances are backend-owned so later lifecycle/timing phases can correlate against
            // a server-generated runtime identity rather than any client-local prediction token.
            var activation = new AuthorityActivationRecord(
                Guid.NewGuid().ToString("N"),
                sessionId,
                entityId,
                abilityId,
                AuthorityActivationPhase.Accepted,
                createdAtUtc,
                createdAtUtc + StartupDuration,
                null);

            if (_activationStore.TryAdd(activation))
            {
                return activation.ActivationInstanceId;
            }
        }
    }

    private static AbilityInterruptionResult RejectInterruption(
        string sessionId,
        string? entityId,
        string? abilityId,
        string? activationInstanceId,
        string? interruptionCode,
        string code,
        string message)
    {
        return new AbilityInterruptionResult(
            false,
            sessionId,
            entityId,
            abilityId,
            activationInstanceId,
            interruptionCode,
            null,
            code,
            message);
    }
}
