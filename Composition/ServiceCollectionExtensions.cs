using MMOAS.AuthorityService.Application.Abilities;
using MMOAS.AuthorityService.Application.Entities;
using MMOAS.AuthorityService.Application.Sessions;
using MMOAS.AuthorityService.Domain.Abilities;
using MMOAS.AuthorityService.Hosting;
using MMOAS.AuthorityService.State;
using MMOAS.AuthorityService.Transport;

namespace MMOAS.AuthorityService.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorityServicePhase02(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAuthorityEntityStore, InMemoryAuthorityEntityStore>();
        services.AddSingleton<IAuthoritySessionStore, InMemoryAuthoritySessionStore>();
        services.AddSingleton<IEntityRegistrationService, EntityRegistrationService>();
        services.AddSingleton<IAuthoritySessionService, AuthoritySessionService>();
        services.AddSingleton<IAbilityActivationValidator, AbilityActivationValidator>();
        services.AddSingleton<IAbilityActivationService, AbilityActivationService>();
        services.AddSingleton<AuthorityWebSocketSessionHandler>();
        services.AddHostedService<AuthorityLifecycleHostedService>();

        return services;
    }
}
