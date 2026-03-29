using System.Text.Json;

namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record AuthorityInboundMessageEnvelope(
    string MessageType,
    int Version,
    string? RequestId,
    JsonElement? Payload);
