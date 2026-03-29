namespace MMOAS.AuthorityService.State;

public interface IAuthorityActivationStore
{
    bool TryAdd(AuthorityActivationRecord activation);

    AuthorityActivationSnapshot GetSnapshot();
}
