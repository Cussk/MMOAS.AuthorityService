namespace MMOAS.AuthorityService.Hosting;

public sealed class AuthorityLifecycleHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(100);
    private readonly AuthorityLifecycleAdvancer _lifecycleAdvancer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthorityLifecycleHostedService> _logger;

    public AuthorityLifecycleHostedService(
        AuthorityLifecycleAdvancer lifecycleAdvancer,
        TimeProvider timeProvider,
        ILogger<AuthorityLifecycleHostedService> logger)
    {
        _lifecycleAdvancer = lifecycleAdvancer;
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
                await _lifecycleAdvancer.AdvanceAsync(_timeProvider.GetUtcNow(), stoppingToken);
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
