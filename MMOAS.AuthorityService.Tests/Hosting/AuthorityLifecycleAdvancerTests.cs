using Microsoft.Extensions.Logging.Abstractions;
using MMOAS.AuthorityService.Hosting;
using MMOAS.AuthorityService.State;
using MMOAS.AuthorityService.Transport;

namespace MMOAS.AuthorityService.Tests.Hosting;

public sealed class AuthorityLifecycleAdvancerTests
{
    [Fact]
    public async Task AdvanceAsync_CommitsEligibleActivationAndPublishesNotification()
    {
        var store = new InMemoryAuthorityActivationStore();
        var notifier = new RecordingSessionNotifier();
        var advancer = new AuthorityLifecycleAdvancer(
            store,
            notifier,
            NullLogger<AuthorityLifecycleAdvancer>.Instance);
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);
        var commitDueAtUtc = createdAtUtc.AddMilliseconds(500);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            commitDueAtUtc,
            null));

        await advancer.AdvanceAsync(commitDueAtUtc, CancellationToken.None);

        var activation = Assert.Single(store.GetSnapshot().Activations);
        var notification = Assert.Single(notifier.Notifications);

        Assert.Equal(AuthorityActivationPhase.Committed, activation.Phase);
        Assert.Equal(commitDueAtUtc, activation.CommittedAtUtc);
        Assert.Equal("session-001", notification.SessionId);
        Assert.Equal("activation-001", notification.ActivationInstanceId);
        Assert.Equal(commitDueAtUtc, notification.CommittedAtUtc);
    }

    [Fact]
    public async Task AdvanceAsync_LeavesCommittedStateEvenWhenDeliveryCannotReachSession()
    {
        var store = new InMemoryAuthorityActivationStore();
        var notifier = new RecordingSessionNotifier(delivered: false);
        var advancer = new AuthorityLifecycleAdvancer(
            store,
            notifier,
            NullLogger<AuthorityLifecycleAdvancer>.Instance);
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);
        var commitDueAtUtc = createdAtUtc.AddMilliseconds(500);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            commitDueAtUtc,
            null));

        await advancer.AdvanceAsync(commitDueAtUtc, CancellationToken.None);

        var activation = Assert.Single(store.GetSnapshot().Activations);

        Assert.Equal(AuthorityActivationPhase.Committed, activation.Phase);
        Assert.Equal(commitDueAtUtc, activation.CommittedAtUtc);
        Assert.Single(notifier.Notifications);
    }

    [Fact]
    public async Task AdvanceAsync_DoesNotRecommitAlreadyCommittedActivation()
    {
        var store = new InMemoryAuthorityActivationStore();
        var notifier = new RecordingSessionNotifier();
        var advancer = new AuthorityLifecycleAdvancer(
            store,
            notifier,
            NullLogger<AuthorityLifecycleAdvancer>.Instance);
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);
        var commitDueAtUtc = createdAtUtc.AddMilliseconds(500);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            commitDueAtUtc,
            null));

        await advancer.AdvanceAsync(commitDueAtUtc, CancellationToken.None);
        await advancer.AdvanceAsync(commitDueAtUtc.AddMilliseconds(100), CancellationToken.None);

        var activation = Assert.Single(store.GetSnapshot().Activations);

        Assert.Equal(AuthorityActivationPhase.Committed, activation.Phase);
        Assert.Equal(commitDueAtUtc, activation.CommittedAtUtc);
        Assert.Single(notifier.Notifications);
    }

    [Fact]
    public async Task AdvanceAsync_DoesNotCommitInterruptedActivation()
    {
        var store = new InMemoryAuthorityActivationStore();
        var notifier = new RecordingSessionNotifier();
        var advancer = new AuthorityLifecycleAdvancer(
            store,
            notifier,
            NullLogger<AuthorityLifecycleAdvancer>.Instance);
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);
        var commitDueAtUtc = createdAtUtc.AddMilliseconds(500);
        var interruptedAtUtc = createdAtUtc.AddMilliseconds(250);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            commitDueAtUtc,
            null));
        store.TryMarkInterrupted("activation-001", "activation.interrupted.manual", interruptedAtUtc);

        await advancer.AdvanceAsync(commitDueAtUtc, CancellationToken.None);

        var activation = Assert.Single(store.GetSnapshot().Activations);

        Assert.Equal(AuthorityActivationPhase.Interrupted, activation.Phase);
        Assert.Equal(interruptedAtUtc, activation.InterruptedAtUtc);
        Assert.Null(activation.CommittedAtUtc);
        Assert.Empty(notifier.Notifications);
    }

    private sealed class RecordingSessionNotifier : IAuthoritySessionNotifier
    {
        private readonly bool _delivered;

        public RecordingSessionNotifier(bool delivered = true)
        {
            _delivered = delivered;
        }

        public List<AbilityCommittedNotification> Notifications { get; } = [];

        public ValueTask<bool> NotifyAbilityCommittedAsync(
            AbilityCommittedNotification notification,
            CancellationToken cancellationToken)
        {
            Notifications.Add(notification);
            return ValueTask.FromResult(_delivered);
        }
    }
}
