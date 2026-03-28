using MMOAS.AuthorityService.Debug.Contracts;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Debug;

public static class DebugEndpointMappings
{
    public static IEndpointRouteBuilder MapDebugEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/debug/health", (TimeProvider timeProvider) =>
            Results.Ok(new DebugHealthResponse("ok", timeProvider.GetUtcNow())));

        endpoints.MapGet("/debug/snapshot", (IAuthorityEntityStore entityStore) =>
        {
            var snapshot = entityStore.GetSnapshot();
            var response = new DebugSnapshotResponse(
                snapshot.Count,
                snapshot.Entities
                    .Select(entity => new DebugEntityResponse(entity.EntityId, entity.RegisteredAtUtc))
                    .ToArray());

            return Results.Ok(response);
        });

        return endpoints;
    }
}
