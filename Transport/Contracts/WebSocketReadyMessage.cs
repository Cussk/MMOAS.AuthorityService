namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record WebSocketReadyMessage(string Type, DateTimeOffset UtcNow);
