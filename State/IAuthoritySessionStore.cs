namespace MMOAS.AuthorityService.State;

public interface IAuthoritySessionStore
{
    AuthoritySessionRecord CreateOrGet(string sessionId, DateTimeOffset connectedAtUtc);

    AuthoritySessionRecord? Get(string sessionId);

    AuthoritySessionRecord? MarkHelloCompleted(string sessionId);

    AuthoritySessionRecord? AssignRegisteredEntity(string sessionId, string entityId);

    bool TryRemove(string sessionId);

    AuthoritySessionSnapshot GetSnapshot();
}
