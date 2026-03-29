namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record WebSocketReadyMessage(string SessionId, DateTimeOffset ConnectedAtUtc, bool HelloCompleted);
