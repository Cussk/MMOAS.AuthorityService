using MMOAS.AuthorityService.State;
using MMOAS.AuthorityService.Transport;

namespace MMOAS.AuthorityService.Hosting;

public sealed class AuthorityLifecycleAdvancer
{
    private readonly IAuthorityActivationStore _activationStore;
    private readonly IAuthoritySessionNotifier _sessionNotifier;
    private readonly ILogger<AuthorityLifecycleAdvancer> _logger;

    public AuthorityLifecycleAdvancer(
        IAuthorityActivationStore activationStore,
        IAuthoritySessionNotifier sessionNotifier,
        ILogger<AuthorityLifecycleAdvancer> logger)
    {
        _activationStore = activationStore;
        _sessionNotifier = sessionNotifier;
        _logger = logger;
    }

    public async ValueTask AdvanceAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = _activationStore.GetSnapshot();

        foreach (var activation in snapshot.Activations)
        {
            if (!IsCommitEligible(activation, utcNow))
            {
                continue;
            }

            var committedActivation = _activationStore.TryMarkCommitted(activation.ActivationInstanceId, utcNow);

            if (committedActivation is null)
            {
                continue;
            }

            // Lifecycle truth is committed in state before delivery. Socket availability cannot own authority.
            var notification = new AbilityCommittedNotification(
                committedActivation.SessionId,
                committedActivation.EntityId,
                committedActivation.AbilityId,
                committedActivation.ActivationInstanceId,
                committedActivation.CommittedAtUtc!.Value);

            try
            {
                var delivered = await _sessionNotifier.NotifyAbilityCommittedAsync(notification, cancellationToken);

                if (!delivered)
                {
                    _logger.LogInformation(
                        "Committed activation {ActivationInstanceId} for session {SessionId} without live delivery",
                        committedActivation.ActivationInstanceId,
                        committedActivation.SessionId);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to deliver commit notification for activation {ActivationInstanceId}",
                    committedActivation.ActivationInstanceId);
            }
        }
    }

    private static bool IsCommitEligible(AuthorityActivationRecord activation, DateTimeOffset utcNow)
    {
        // Interrupted activations stay terminal in this phase because only Accepted can advance on the timed path.
        return activation.Phase == AuthorityActivationPhase.Accepted
               && utcNow >= activation.CommitDueAtUtc;
    }
}
