using MMOAS.AuthorityService.Application.Abilities;
using MMOAS.AuthorityService.Domain.Abilities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.Application.Abilities;

public sealed class AbilityActivationServiceTests
{
    [Fact]
    public async Task ActivateAsync_RejectsUnknownSessionWithoutCreatingActivation()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        var result = await service.ActivateAsync("session-001", "ability.basic", CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Null(result.ActivationInstanceId);
        Assert.Equal("activation.session-not-found", result.Code);
        Assert.Equal(0, activationStore.GetSnapshot().Count);
    }

    [Fact]
    public async Task ActivateAsync_CreatesBackendOwnedActivationInstanceWhenAccepted()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        sessionStore.CreateOrGet("session-001", new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        sessionStore.MarkHelloCompleted("session-001");
        sessionStore.AssignRegisteredEntity("session-001", "entity-001");

        var result = await service.ActivateAsync("session-001", " ability.basic ", CancellationToken.None);
        var snapshot = activationStore.GetSnapshot();

        Assert.True(result.Accepted);
        Assert.NotNull(result.ActivationInstanceId);
        Assert.Equal("ability.basic", result.AbilityId);
        Assert.Single(snapshot.Activations);
        Assert.Equal(result.ActivationInstanceId, snapshot.Activations[0].ActivationInstanceId);
        Assert.Equal("session-001", snapshot.Activations[0].SessionId);
        Assert.Equal("entity-001", snapshot.Activations[0].EntityId);
        Assert.Equal(AuthorityActivationPhase.Accepted, snapshot.Activations[0].Phase);
        Assert.Equal(timeProvider.GetUtcNow(), snapshot.Activations[0].CreatedAtUtc);
        Assert.Equal(timeProvider.GetUtcNow().AddMilliseconds(500), snapshot.Activations[0].CommitDueAtUtc);
        Assert.Null(snapshot.Activations[0].CommittedAtUtc);
    }

    [Fact]
    public async Task ActivateAsync_RejectsWhitespaceOnlyAbilityIdWithoutCreatingActivation()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        sessionStore.CreateOrGet("session-001", new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        sessionStore.MarkHelloCompleted("session-001");
        sessionStore.AssignRegisteredEntity("session-001", "entity-001");

        var result = await service.ActivateAsync("session-001", "   ", CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Null(result.ActivationInstanceId);
        Assert.Equal("activation.invalid-ability", result.Code);
        Assert.Equal(0, activationStore.GetSnapshot().Count);
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
