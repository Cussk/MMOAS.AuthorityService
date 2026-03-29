namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record AuthorityOutboundMessageEnvelope(
    string MessageType,
    int Version,
    DateTimeOffset ServerUtcNow,
    string? RequestId,
    object Payload);
