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

    public AuthorityActivationSnapshot GetSnapshot()
    {
        var snapshot = _activations.Values
            .OrderBy(activation => activation.CreatedAtUtc)
            .ThenBy(activation => activation.ActivationInstanceId, StringComparer.Ordinal)
            .ToArray();

        return new AuthorityActivationSnapshot(snapshot);
    }
}
