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

    public AuthorityActivationRecord? TryMarkCommitted(string activationInstanceId, DateTimeOffset committedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activationInstanceId);

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

            var committedActivation = existingActivation with
            {
                Phase = AuthorityActivationPhase.Committed,
                CommittedAtUtc = committedAtUtc
            };

            // Compare-and-swap keeps commit one-shot even while hosted lifecycle ticks race with debug reads.
            if (_activations.TryUpdate(activationInstanceId, committedActivation, existingActivation))
            {
                return committedActivation;
            }
        }
    }

    public AuthorityActivationSnapshot GetSnapshot()
    {
        var snapshot = _activations.Values
            .OrderBy(activation => activation.CreatedAtUtc)
            .ThenBy(activation => activation.ActivationInstanceId, StringComparer.Ordinal)
            .ToArray();

        return new AuthorityActivationSnapshot(snapshot);
    }
}
