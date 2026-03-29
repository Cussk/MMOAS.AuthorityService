namespace MMOAS.AuthorityService.State;

public sealed record AuthoritySessionSnapshot(IReadOnlyList<AuthoritySessionRecord> Sessions)
{
    public int Count => Sessions.Count;
}
