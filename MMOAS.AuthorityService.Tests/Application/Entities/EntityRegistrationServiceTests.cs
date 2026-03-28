using MMOAS.AuthorityService.Application.Entities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.Application.Entities;

public sealed class EntityRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_GeneratesBackendOwnedEntityRecord()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 28, 12, 00, 00, TimeSpan.Zero));

        var entityStore = new InMemoryAuthorityEntityStore();
        var service = new EntityRegistrationService(entityStore, timeProvider);

        var registeredEntity = await service.RegisterAsync(CancellationToken.None);
        var snapshot = entityStore.GetSnapshot();

        Assert.NotNull(registeredEntity.EntityId);
        Assert.NotEmpty(registeredEntity.EntityId);
        Assert.Equal(timeProvider.GetUtcNow(), registeredEntity.RegisteredAtUtc);
        Assert.Single(snapshot.Entities);
        Assert.Equal(registeredEntity.EntityId, snapshot.Entities[0].EntityId);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
