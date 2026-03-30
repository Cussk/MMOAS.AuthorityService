namespace MMOAS.AuthorityService.State;

public interface IAuthorityActivationStore
{
    bool TryAdd(AuthorityActivationRecord activation);

    AuthorityActivationRecord? Get(string activationInstanceId);

    AuthorityActivationRecord? TryMarkCommitted(string activationInstanceId, DateTimeOffset committedAtUtc);

    AuthorityActivationRecord? TryMarkInterrupted(
        string activationInstanceId,
        string interruptionCode,
        DateTimeOffset interruptedAtUtc);

    AuthorityActivationSnapshot GetSnapshot();
}
