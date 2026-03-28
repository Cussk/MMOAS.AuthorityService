using MMOAS.AuthorityService.Application.Entities;
using MMOAS.AuthorityService.Hosting;
using MMOAS.AuthorityService.State;
using MMOAS.AuthorityService.Transport;

namespace MMOAS.AuthorityService.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorityServicePhase00(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAuthorityEntityStore, InMemoryAuthorityEntityStore>();
        services.AddSingleton<IEntityRegistrationService, EntityRegistrationService>();
        services.AddSingleton<AuthorityWebSocketSessionHandler>();
        services.AddHostedService<AuthorityLifecycleHostedService>();

        return services;
    }
}
