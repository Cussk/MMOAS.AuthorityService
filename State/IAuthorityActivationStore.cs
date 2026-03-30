namespace MMOAS.AuthorityService.State;

public interface IAuthorityActivationStore
{
    bool TryAdd(AuthorityActivationRecord activation);

    AuthorityActivationRecord? TryMarkCommitted(string activationInstanceId, DateTimeOffset committedAtUtc);

    AuthorityActivationSnapshot GetSnapshot();
}
