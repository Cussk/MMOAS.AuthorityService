using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.State;

public sealed class InMemoryAuthorityEntityStoreTests
{
    [Fact]
    public void TryRegister_AddsEntityToSnapshot()
    {
        var store = new InMemoryAuthorityEntityStore();
        var registeredAtUtc = new DateTimeOffset(2026, 03, 28, 12, 00, 00, TimeSpan.Zero);

        var added = store.TryRegister(new AuthorityEntityRecord("entity-001", registeredAtUtc));
        var snapshot = store.GetSnapshot();

        Assert.True(added);
        Assert.Equal(1, snapshot.Count);
        Assert.Equal("entity-001", snapshot.Entities[0].EntityId);
        Assert.Equal(registeredAtUtc, snapshot.Entities[0].RegisteredAtUtc);
    }

    [Fact]
    public void GetSnapshot_ReturnsStableCopy()
    {
        var store = new InMemoryAuthorityEntityStore();

        store.TryRegister(new AuthorityEntityRecord("entity-001", new DateTimeOffset(2026, 03, 28, 12, 00, 00, TimeSpan.Zero)));
        var firstSnapshot = store.GetSnapshot();

        store.TryRegister(new AuthorityEntityRecord("entity-002", new DateTimeOffset(2026, 03, 28, 12, 00, 01, TimeSpan.Zero)));
        var secondSnapshot = store.GetSnapshot();

        Assert.Single(firstSnapshot.Entities);
        Assert.Equal(2, secondSnapshot.Count);
    }

    [Fact]
    public void TryRegister_IsThreadSafeUnderConcurrentWriters()
    {
        var store = new InMemoryAuthorityEntityStore();
        var registeredAtUtc = new DateTimeOffset(2026, 03, 28, 12, 00, 00, TimeSpan.Zero);

        Parallel.For(0, 256, index =>
        {
            store.TryRegister(new AuthorityEntityRecord($"entity-{index:D3}", registeredAtUtc.AddMilliseconds(index)));
        });

        var snapshot = store.GetSnapshot();

        Assert.Equal(256, snapshot.Count);
    }
}
