using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Application.Entities;

public sealed class EntityRegistrationService : IEntityRegistrationService
{
    private readonly IAuthorityEntityStore _entityStore;
    private readonly TimeProvider _timeProvider;

    public EntityRegistrationService(IAuthorityEntityStore entityStore, TimeProvider timeProvider)
    {
        _entityStore = entityStore;
        _timeProvider = timeProvider;
    }

    public ValueTask<RegisteredEntity> RegisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        while (true)
        {
            // Phase 03 still keeps backend-generated IDs and backend time authoritative for registration.
            var entity = new AuthorityEntityRecord(
                Guid.NewGuid().ToString("N"),
                _timeProvider.GetUtcNow());

            if (_entityStore.TryRegister(entity))
            {
                return ValueTask.FromResult(new RegisteredEntity(entity.EntityId, entity.RegisteredAtUtc));
            }
        }
    }
}
