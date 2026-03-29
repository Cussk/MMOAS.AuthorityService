namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugSessionSnapshotResponse(int SessionCount, IReadOnlyList<DebugSessionResponse> Sessions);
