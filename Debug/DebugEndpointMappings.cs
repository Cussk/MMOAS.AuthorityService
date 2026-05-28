using MMOAS.AuthorityService.Debug.Contracts;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Debug;

public static class DebugEndpointMappings
{
    public static IEndpointRouteBuilder MapDebugEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", GetHealth);
        endpoints.MapGet("/debug/health", GetHealth);

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

        endpoints.MapGet("/debug/sessions", (IAuthoritySessionStore sessionStore) =>
        {
            var snapshot = sessionStore.GetSnapshot();
            var response = new DebugSessionSnapshotResponse(
                snapshot.Count,
                snapshot.Sessions
                    .Select(session => new DebugSessionResponse(
                        session.SessionId,
                        session.ConnectedAtUtc,
                        session.HelloCompleted,
                        session.RegisteredEntityId))
                    .ToArray());

            return Results.Ok(response);
        });

        endpoints.MapGet("/debug/activations", (IAuthorityActivationStore activationStore) =>
        {
            var snapshot = activationStore.GetSnapshot();
            var response = new DebugActivationSnapshotResponse(
                snapshot.Count,
                snapshot.Activations
                    .Select(activation => new DebugActivationResponse(
                        activation.ActivationInstanceId,
                        activation.SessionId,
                        activation.EntityId,
                        activation.AbilityId,
                        activation.Phase.ToString(),
                        activation.CreatedAtUtc,
                        activation.CommitDueAtUtc,
                        activation.CommittedAtUtc,
                        activation.InterruptionCode,
                        activation.InterruptedAtUtc))
                    .ToArray());

            return Results.Ok(response);
        });

        return endpoints;
    }

    private static IResult GetHealth(TimeProvider timeProvider) =>
        Results.Ok(new DebugHealthResponse("ok", timeProvider.GetUtcNow()));
}
