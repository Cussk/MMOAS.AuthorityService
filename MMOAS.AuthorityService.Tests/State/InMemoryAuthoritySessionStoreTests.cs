using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.State;

public sealed class InMemoryAuthoritySessionStoreTests
{
    [Fact]
    public void CreateOrGet_AddsSessionToSnapshot()
    {
        var store = new InMemoryAuthoritySessionStore();
        var connectedAtUtc = new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero);

        var session = store.CreateOrGet("session-001", connectedAtUtc);
        var snapshot = store.GetSnapshot();

        Assert.Equal("session-001", session.SessionId);
        Assert.Equal(connectedAtUtc, session.ConnectedAtUtc);
        Assert.False(session.HelloCompleted);
        Assert.Null(session.RegisteredEntityId);
        Assert.Single(snapshot.Sessions);
        Assert.Equal("session-001", snapshot.Sessions[0].SessionId);
    }

    [Fact]
    public void MarkHelloCompleted_UpdatesExistingSession()
    {
        var store = new InMemoryAuthoritySessionStore();

        store.CreateOrGet("session-001", new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        var updatedSession = store.MarkHelloCompleted("session-001");

        Assert.NotNull(updatedSession);
        Assert.True(updatedSession.HelloCompleted);
        Assert.True(store.Get("session-001")!.HelloCompleted);
    }

    [Fact]
    public void AssignRegisteredEntity_UpdatesSessionAssociation()
    {
        var store = new InMemoryAuthoritySessionStore();

        store.CreateOrGet("session-001", new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        var updatedSession = store.AssignRegisteredEntity("session-001", "entity-001");

        Assert.NotNull(updatedSession);
        Assert.Equal("entity-001", updatedSession.RegisteredEntityId);
        Assert.Equal("entity-001", store.Get("session-001")!.RegisteredEntityId);
    }

    [Fact]
    public void CreateOrGet_IsThreadSafeUnderConcurrentWriters()
    {
        var store = new InMemoryAuthoritySessionStore();
        var connectedAtUtc = new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero);

        Parallel.For(0, 256, index =>
        {
            store.CreateOrGet($"session-{index:D3}", connectedAtUtc.AddMilliseconds(index));
        });

        var snapshot = store.GetSnapshot();

        Assert.Equal(256, snapshot.Count);
    }
}
