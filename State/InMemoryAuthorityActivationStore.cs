using System.Collections.Concurrent;

namespace MMOAS.AuthorityService.State;

public sealed class InMemoryAuthorityActivationStore : IAuthorityActivationStore
{
    private readonly ConcurrentDictionary<string, AuthorityActivationRecord> _activations = new(StringComparer.Ordinal);

    public bool TryAdd(AuthorityActivationRecord activation)
    {
        ArgumentNullException.ThrowIfNull(activation);

        return _activations.TryAdd(activation.ActivationInstanceId, activation);
    }

    public AuthorityActivationRecord? Get(string activationInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activationInstanceId);

        return _activations.TryGetValue(activationInstanceId, out var activation) ? activation : null;
    }

    public AuthorityActivationRecord? TryMarkCommitted(string activationInstanceId, DateTimeOffset committedAtUtc)
    {
        return TryTransitionAcceptedActivation(
            activationInstanceId,
            existingActivation => existingActivation with
            {
                Phase = AuthorityActivationPhase.Committed,
                CommittedAtUtc = committedAtUtc
            });
    }

    public AuthorityActivationRecord? TryMarkInterrupted(
        string activationInstanceId,
        string interruptionCode,
        DateTimeOffset interruptedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(interruptionCode);

        return TryTransitionAcceptedActivation(
            activationInstanceId,
            existingActivation => existingActivation with
            {
                Phase = AuthorityActivationPhase.Interrupted,
                InterruptionCode = interruptionCode,
                InterruptedAtUtc = interruptedAtUtc
            });
    }

    public AuthorityActivationSnapshot GetSnapshot()
    {
        var snapshot = _activations.Values
            .OrderBy(activation => activation.CreatedAtUtc)
            .ThenBy(activation => activation.ActivationInstanceId, StringComparer.Ordinal)
            .ToArray();

        return new AuthorityActivationSnapshot(snapshot);
    }

    private AuthorityActivationRecord? TryTransitionAcceptedActivation(
        string activationInstanceId,
        Func<AuthorityActivationRecord, AuthorityActivationRecord> transition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activationInstanceId);
        ArgumentNullException.ThrowIfNull(transition);

        while (true)
        {
            if (!_activations.TryGetValue(activationInstanceId, out var existingActivation))
            {
                return null;
            }

            if (existingActivation.Phase != AuthorityActivationPhase.Accepted)
            {
                return null;
            }

            var updatedActivation = transition(existingActivation);

            // Compare-and-swap keeps Accepted -> terminal transitions one-shot while hosted commit ticks race
            // against application-driven interruption requests.
            if (_activations.TryUpdate(activationInstanceId, updatedActivation, existingActivation))
            {
                return updatedActivation;
            }
        }
    }
}
