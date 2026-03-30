namespace MMOAS.AuthorityService.Transport;

public interface IAuthoritySessionNotifier
{
    ValueTask<bool> NotifyAbilityCommittedAsync(
        AbilityCommittedNotification notification,
        CancellationToken cancellationToken);
}
