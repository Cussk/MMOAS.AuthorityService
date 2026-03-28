namespace MMOAS.AuthorityService.Debug.Contracts;

public sealed record DebugHealthResponse(string Status, DateTimeOffset UtcNow);
