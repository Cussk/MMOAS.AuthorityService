namespace MMOAS.AuthorityService.Hosting;

public sealed class AuthorityLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthorityLifecycleHostedService> _logger;

    public AuthorityLifecycleHostedService(
        TimeProvider timeProvider,
        ILogger<AuthorityLifecycleHostedService> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Authority lifecycle hosted service started at {StartedAtUtc}",
            _timeProvider.GetUtcNow());

        using var timer = new PeriodicTimer(TickInterval, _timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                // Phase 01 still intentionally avoids lifecycle mutation. This loop exists only to establish the
                // backend-owned clock boundary that later phases will build on.
                _logger.LogDebug(
                    "Authority lifecycle stub tick at {TickAtUtc}",
                    _timeProvider.GetUtcNow());
            }
        }
        catch (OperationCanceledException)
        {
            // Background service shutdown is cooperative and expected during host stop.
        }

        _logger.LogInformation(
            "Authority lifecycle hosted service stopped at {StoppedAtUtc}",
            _timeProvider.GetUtcNow());
    }
}
