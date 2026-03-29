namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugActivationSnapshotResponse(int ActivationCount, IReadOnlyList<DebugActivationResponse> Activations);
