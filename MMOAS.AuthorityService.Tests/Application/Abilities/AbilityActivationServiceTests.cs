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

    [Fact]
    public async Task InterruptAsync_MarksAcceptedActivationInterrupted()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        activationStore.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            timeProvider.GetUtcNow().AddMilliseconds(-100),
            timeProvider.GetUtcNow().AddMilliseconds(400),
            null));

        var result = await service.InterruptAsync(
            "session-001",
            " activation-001 ",
            " activation.interrupted.manual ",
            CancellationToken.None);
        var activation = Assert.Single(activationStore.GetSnapshot().Activations);

        Assert.True(result.Interrupted);
        Assert.Equal("activation-001", result.ActivationInstanceId);
        Assert.Equal("activation.interrupted.manual", result.InterruptionCode);
        Assert.Equal(AuthorityActivationPhase.Interrupted, activation.Phase);
        Assert.Equal(timeProvider.GetUtcNow(), activation.InterruptedAtUtc);
        Assert.Equal("activation.interrupted.manual", activation.InterruptionCode);
    }

    [Fact]
    public async Task InterruptAsync_RejectsEmptyInterruptionCodeWithoutMutatingState()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        activationStore.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            timeProvider.GetUtcNow().AddMilliseconds(-100),
            timeProvider.GetUtcNow().AddMilliseconds(400),
            null));

        var result = await service.InterruptAsync(
            "session-001",
            "activation-001",
            "   ",
            CancellationToken.None);
        var activation = Assert.Single(activationStore.GetSnapshot().Activations);

        Assert.False(result.Interrupted);
        Assert.Equal("activation.interrupt.invalid-code", result.Code);
        Assert.Equal(AuthorityActivationPhase.Accepted, activation.Phase);
        Assert.Null(activation.InterruptedAtUtc);
    }

    [Fact]
    public async Task InterruptAsync_RejectsInterruptForDifferentSession()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        activationStore.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-owner",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            timeProvider.GetUtcNow().AddMilliseconds(-100),
            timeProvider.GetUtcNow().AddMilliseconds(400),
            null));

        var result = await service.InterruptAsync(
            "session-other",
            "activation-001",
            "activation.interrupted.manual",
            CancellationToken.None);
        var activation = Assert.Single(activationStore.GetSnapshot().Activations);

        Assert.False(result.Interrupted);
        Assert.Equal("activation.interrupt.session-mismatch", result.Code);
        Assert.Equal(AuthorityActivationPhase.Accepted, activation.Phase);
        Assert.Null(activation.InterruptedAtUtc);
    }

    [Fact]
    public async Task InterruptAsync_RejectsAlreadyCommittedActivation()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero));
        var activationStore = new InMemoryAuthorityActivationStore();
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(activationStore, sessionStore, validator, timeProvider);

        activationStore.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Committed,
            timeProvider.GetUtcNow().AddMilliseconds(-500),
            timeProvider.GetUtcNow().AddMilliseconds(-100),
            timeProvider.GetUtcNow().AddMilliseconds(-100)));

        var result = await service.InterruptAsync(
            "session-001",
            "activation-001",
            "activation.interrupted.manual",
            CancellationToken.None);

        Assert.False(result.Interrupted);
        Assert.Equal("activation.interrupt.not-interruptible", result.Code);
        Assert.Contains("Committed", result.Message, StringComparison.Ordinal);
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
