namespace MMOAS.AuthorityService.State;

public sealed record AuthorityActivationSnapshot(IReadOnlyList<AuthorityActivationRecord> Activations)
{
    public int Count => Activations.Count;
}
