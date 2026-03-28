namespace MMOAS.AuthorityService.State;

public interface IAuthorityEntityStore
{
    bool TryRegister(AuthorityEntityRecord entity);

    AuthorityEntitySnapshot GetSnapshot();
}
