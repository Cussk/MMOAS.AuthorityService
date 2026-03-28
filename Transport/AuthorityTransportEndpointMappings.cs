using MMOAS.AuthorityService.Application.Entities;
using MMOAS.AuthorityService.Transport.Contracts;

namespace MMOAS.AuthorityService.Transport;

public static class AuthorityTransportEndpointMappings
{
    public static IEndpointRouteBuilder MapAuthorityTransportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/transport");

        group.MapPost("/entities", async (IEntityRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            var registeredEntity = await registrationService.RegisterAsync(cancellationToken);
            var response = new RegisterEntityHttpResponse(
                registeredEntity.EntityId,
                registeredEntity.RegisteredAtUtc);

            return Results.Ok(response);
        });

        group.MapGet("/ws", (HttpContext context, AuthorityWebSocketSessionHandler sessionHandler, CancellationToken cancellationToken) =>
            sessionHandler.HandleAsync(context, cancellationToken));

        return endpoints;
    }
}
