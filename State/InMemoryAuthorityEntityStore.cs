using System.Collections.Concurrent;

namespace MMOAS.AuthorityService.State;

public sealed class InMemoryAuthorityEntityStore : IAuthorityEntityStore
{
    private readonly ConcurrentDictionary<string, AuthorityEntityRecord> _entities = new(StringComparer.Ordinal);

    public bool TryRegister(AuthorityEntityRecord entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return _entities.TryAdd(entity.EntityId, entity);
    }

    public AuthorityEntitySnapshot GetSnapshot()
    {
        var snapshot = _entities.Values
            .OrderBy(entity => entity.RegisteredAtUtc)
            .ThenBy(entity => entity.EntityId, StringComparer.Ordinal)
            .ToArray();

        return new AuthorityEntitySnapshot(snapshot);
    }
}
