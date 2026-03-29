using MMOAS.AuthorityService.Application.Entities;
using MMOAS.AuthorityService.Application.Sessions;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.Application.Sessions;

public sealed class AuthoritySessionServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_StoresConnectionWithBackendTime()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        var entityStore = new InMemoryAuthorityEntityStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var registrationService = new EntityRegistrationService(entityStore, timeProvider);
        var sessionService = new AuthoritySessionService(sessionStore, registrationService, timeProvider);

        await sessionService.CreateSessionAsync("session-001", CancellationToken.None);
        var session = sessionStore.Get("session-001");

        Assert.NotNull(session);
        Assert.Equal(timeProvider.GetUtcNow(), session.ConnectedAtUtc);
        Assert.False(session.HelloCompleted);
    }

    [Fact]
    public async Task CompleteHelloAsync_MarksSessionAsReady()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        var entityStore = new InMemoryAuthorityEntityStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var registrationService = new EntityRegistrationService(entityStore, timeProvider);
        var sessionService = new AuthoritySessionService(sessionStore, registrationService, timeProvider);

        await sessionService.CreateSessionAsync("session-001", CancellationToken.None);
        var helloCompleted = await sessionService.CompleteHelloAsync("session-001", CancellationToken.None);

        Assert.True(helloCompleted.HelloCompleted);
        Assert.True(sessionStore.Get("session-001")!.HelloCompleted);
    }

    [Fact]
    public async Task RegisterEntityAsync_RequiresHelloBeforeRegistration()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        var entityStore = new InMemoryAuthorityEntityStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var registrationService = new EntityRegistrationService(entityStore, timeProvider);
        var sessionService = new AuthoritySessionService(sessionStore, registrationService, timeProvider);

        await sessionService.CreateSessionAsync("session-001", CancellationToken.None);

        var exception = await Assert.ThrowsAsync<AuthoritySessionException>(() =>
            sessionService.RegisterEntityAsync("session-001", CancellationToken.None).AsTask());

        Assert.Equal("session.hello-required", exception.Code);
        Assert.Equal(0, entityStore.GetSnapshot().Count);
    }

    [Fact]
    public async Task RegisterEntityAsync_AssignsRegisteredEntityToSession()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        var entityStore = new InMemoryAuthorityEntityStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var registrationService = new EntityRegistrationService(entityStore, timeProvider);
        var sessionService = new AuthoritySessionService(sessionStore, registrationService, timeProvider);

        await sessionService.CreateSessionAsync("session-001", CancellationToken.None);
        await sessionService.CompleteHelloAsync("session-001", CancellationToken.None);

        var registeredEntity = await sessionService.RegisterEntityAsync("session-001", CancellationToken.None);
        var session = sessionStore.Get("session-001");
        var snapshot = entityStore.GetSnapshot();

        Assert.NotNull(session);
        Assert.Equal(registeredEntity.EntityId, session.RegisteredEntityId);
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
