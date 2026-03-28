namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugSnapshotResponse(int EntityCount, IReadOnlyList<DebugEntityResponse> Entities);
