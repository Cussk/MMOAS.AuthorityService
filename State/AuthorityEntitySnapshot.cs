namespace MMOAS.AuthorityService.State;

public sealed record AuthorityEntitySnapshot(IReadOnlyList<AuthorityEntityRecord> Entities)
{
    public int Count => Entities.Count;
}
