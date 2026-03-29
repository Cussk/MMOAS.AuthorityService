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
            createdAtUtc));
        var snapshot = store.GetSnapshot();

        Assert.True(added);
        Assert.Single(snapshot.Activations);
        Assert.Equal("activation-001", snapshot.Activations[0].ActivationInstanceId);
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
            new DateTimeOffset(2026, 03, 30, 12, 00, 00, TimeSpan.Zero)));
        var firstSnapshot = store.GetSnapshot();

        store.TryAdd(new AuthorityActivationRecord(
            "activation-002",
            "session-001",
            "entity-001",
            "ability.basic",
            new DateTimeOffset(2026, 03, 30, 12, 00, 01, TimeSpan.Zero)));
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
                createdAtUtc.AddMilliseconds(index)));
        });

        var snapshot = store.GetSnapshot();

        Assert.Equal(256, snapshot.Count);
    }
}
