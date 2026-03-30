using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.State;

public sealed class InMemoryAuthorityActivationStoreTests
{
    [Fact]
    public void TryAdd_AddsActivationToSnapshot()
    {
        var store = new InMemoryAuthorityActivationStore();
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);

        var added = store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            createdAtUtc.AddMilliseconds(500),
            null));
        var snapshot = store.GetSnapshot();

        Assert.True(added);
        Assert.Single(snapshot.Activations);
        Assert.Equal("activation-001", snapshot.Activations[0].ActivationInstanceId);
        Assert.Equal(AuthorityActivationPhase.Accepted, snapshot.Activations[0].Phase);
        Assert.Equal(createdAtUtc, snapshot.Activations[0].CreatedAtUtc);
    }

    [Fact]
    public void GetSnapshot_ReturnsStableCopy()
    {
        var store = new InMemoryAuthorityActivationStore();

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero).AddMilliseconds(500),
            null));
        var firstSnapshot = store.GetSnapshot();

        store.TryAdd(new AuthorityActivationRecord(
            "activation-002",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            new DateTimeOffset(2026, 03, 30, 12, 00, 01, TimeSpan.Zero),
            new DateTimeOffset(2026, 03, 30, 12, 00, 01, TimeSpan.Zero).AddMilliseconds(500),
            null));
        var secondSnapshot = store.GetSnapshot();

        Assert.Single(firstSnapshot.Activations);
        Assert.Equal(2, secondSnapshot.Count);
    }

    [Fact]
    public void TryAdd_IsThreadSafeUnderConcurrentWriters()
    {
        var store = new InMemoryAuthorityActivationStore();
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);

        Parallel.For(0, 256, index =>
        {
            store.TryAdd(new AuthorityActivationRecord(
                $"activation-{index:D3}",
                $"session-{index:D3}",
                $"entity-{index:D3}",
                "ability.basic",
                AuthorityActivationPhase.Accepted,
                createdAtUtc.AddMilliseconds(index),
                createdAtUtc.AddMilliseconds(index + 500),
                null));
        });

        var snapshot = store.GetSnapshot();

        Assert.Equal(256, snapshot.Count);
    }

    [Fact]
    public void TryMarkCommitted_CommitsActivationOnlyOnce()
    {
        var store = new InMemoryAuthorityActivationStore();
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            createdAtUtc.AddMilliseconds(500),
            null));

        var firstCommit = store.TryMarkCommitted("activation-001", createdAtUtc.AddMilliseconds(500));
        var secondCommit = store.TryMarkCommitted("activation-001", createdAtUtc.AddMilliseconds(600));
        var committedActivation = Assert.Single(store.GetSnapshot().Activations);

        Assert.NotNull(firstCommit);
        Assert.Null(secondCommit);
        Assert.Equal(AuthorityActivationPhase.Committed, committedActivation.Phase);
        Assert.Equal(createdAtUtc.AddMilliseconds(500), committedActivation.CommittedAtUtc);
    }

    [Fact]
    public void TryMarkInterrupted_InterruptsActivationOnlyOnce()
    {
        var store = new InMemoryAuthorityActivationStore();
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);
        var interruptedAtUtc = createdAtUtc.AddMilliseconds(250);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            createdAtUtc.AddMilliseconds(500),
            null));

        var firstInterruption = store.TryMarkInterrupted("activation-001", "activation.interrupted.manual", interruptedAtUtc);
        var secondInterruption = store.TryMarkInterrupted("activation-001", "activation.interrupted.retry", interruptedAtUtc.AddMilliseconds(10));
        var interruptedActivation = Assert.Single(store.GetSnapshot().Activations);

        Assert.NotNull(firstInterruption);
        Assert.Null(secondInterruption);
        Assert.Equal(AuthorityActivationPhase.Interrupted, interruptedActivation.Phase);
        Assert.Equal("activation.interrupted.manual", interruptedActivation.InterruptionCode);
        Assert.Equal(interruptedAtUtc, interruptedActivation.InterruptedAtUtc);
        Assert.Null(interruptedActivation.CommittedAtUtc);
    }

    [Fact]
    public void TryMarkCommitted_DoesNotCommitInterruptedActivation()
    {
        var store = new InMemoryAuthorityActivationStore();
        var createdAtUtc = new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero);
        var interruptedAtUtc = createdAtUtc.AddMilliseconds(250);

        store.TryAdd(new AuthorityActivationRecord(
            "activation-001",
            "session-001",
            "entity-001",
            "ability.basic",
            AuthorityActivationPhase.Accepted,
            createdAtUtc,
            createdAtUtc.AddMilliseconds(500),
            null));

        var interruptedActivation = store.TryMarkInterrupted("activation-001", "activation.interrupted.manual", interruptedAtUtc);
        var committedActivation = store.TryMarkCommitted("activation-001", createdAtUtc.AddMilliseconds(500));
        var snapshotActivation = Assert.Single(store.GetSnapshot().Activations);

        Assert.NotNull(interruptedActivation);
        Assert.Null(committedActivation);
        Assert.Equal(AuthorityActivationPhase.Interrupted, snapshotActivation.Phase);
        Assert.Equal(interruptedAtUtc, snapshotActivation.InterruptedAtUtc);
        Assert.Null(snapshotActivation.CommittedAtUtc);
    }
}
