using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MMOAS.AuthorityService.Transport;
using MMOAS.AuthorityService.Transport.Contracts;

var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var indentedSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};

var options = TestClientOptions.Parse(args);

using var socket = new ClientWebSocket();
using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
string? lastAcceptedActivationInstanceId = null;

Console.WriteLine($"Connecting to {options.WebSocketUrl}");
await socket.ConnectAsync(new Uri(options.WebSocketUrl), cancellationSource.Token);
Console.WriteLine("Connected.");

try
{
    // The client runs the same narrow manual smoke flow each time so local validation stays repeatable.
    await SendAndReceiveAsync(
        socket,
        new AuthorityInboundMessageEnvelope(
            AuthorityTransportProtocol.HelloMessageType,
            AuthorityTransportProtocol.Version,
            "hello-001",
            JsonSerializer.SerializeToElement(new HelloCommand(), serializerOptions)),
        cancellationSource.Token);

    if (options.ActivateBeforeRegister)
    {
        await SendAndReceiveAsync(
            socket,
            new AuthorityInboundMessageEnvelope(
                AuthorityTransportProtocol.ActivateAbilityMessageType,
                AuthorityTransportProtocol.Version,
                "activate-before-register-001",
                JsonSerializer.SerializeToElement(new ActivateAbilityCommand(options.AbilityId), serializerOptions)),
            cancellationSource.Token);
    }

    await SendAndReceiveAsync(
        socket,
        new AuthorityInboundMessageEnvelope(
            AuthorityTransportProtocol.RegisterEntityMessageType,
            AuthorityTransportProtocol.Version,
            "register-001",
            JsonSerializer.SerializeToElement(new RegisterEntityCommand(), serializerOptions)),
        cancellationSource.Token);

    await SendAndReceiveAsync(
        socket,
        new AuthorityInboundMessageEnvelope(
            AuthorityTransportProtocol.ActivateAbilityMessageType,
            AuthorityTransportProtocol.Version,
            "activate-001",
            JsonSerializer.SerializeToElement(new ActivateAbilityCommand(options.AbilityId), serializerOptions)),
        cancellationSource.Token);

    Console.WriteLine("Waiting for authoritative commit event...");
    var committedEnvelope = await ReceiveAndPrintAsync(socket, cancellationSource.Token);
    EnsureCommittedActivationMatches(committedEnvelope);
}
finally
{
    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
    {
        await socket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Test client complete.",
            CancellationToken.None);
    }
}

async Task SendAndReceiveAsync(
    ClientWebSocket clientSocket,
    AuthorityInboundMessageEnvelope envelope,
    CancellationToken cancellationToken)
{
    var outboundJson = JsonSerializer.Serialize(envelope, indentedSerializerOptions);
    Console.WriteLine($">>> outbound {envelope.MessageType}");
    Console.WriteLine(outboundJson);

    var outboundBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, serializerOptions));
    await clientSocket.SendAsync(outboundBytes, WebSocketMessageType.Text, true, cancellationToken);

    await ReceiveAndPrintAsync(clientSocket, cancellationToken);
}

async Task<ClientInboundEnvelope?> ReceiveAndPrintAsync(
    ClientWebSocket clientSocket,
    CancellationToken cancellationToken)
{
    var inboundJson = await ReceiveMessageAsync(clientSocket, cancellationToken);
    var inboundEnvelope = JsonSerializer.Deserialize<ClientInboundEnvelope>(inboundJson, serializerOptions);

    Console.WriteLine($"<<< inbound {inboundEnvelope?.MessageType ?? "unknown"}");
    Console.WriteLine(PrettyPrintJson(inboundJson));
    PrintActivationSummary(inboundEnvelope);
    Console.WriteLine();

    return inboundEnvelope;
}

async Task<string> ReceiveMessageAsync(ClientWebSocket clientSocket, CancellationToken cancellationToken)
{
    var buffer = new byte[4096];
    using var messageBuffer = new MemoryStream();

    while (true)
    {
        var received = await clientSocket.ReceiveAsync(buffer, cancellationToken);

        if (received.MessageType == WebSocketMessageType.Close)
        {
            throw new InvalidOperationException("The server closed the socket before sending a response.");
        }

        if (received.Count > 0)
        {
            messageBuffer.Write(buffer, 0, received.Count);
        }

        if (received.EndOfMessage)
        {
            break;
        }
    }

    return Encoding.UTF8.GetString(messageBuffer.GetBuffer().AsSpan(0, (int)messageBuffer.Length));
}

string PrettyPrintJson(string json)
{
    using var document = JsonDocument.Parse(json);
    return JsonSerializer.Serialize(document.RootElement, indentedSerializerOptions);
}

void PrintActivationSummary(ClientInboundEnvelope? inboundEnvelope)
{
    if (inboundEnvelope is null)
    {
        return;
    }

    if (inboundEnvelope.MessageType == AuthorityTransportProtocol.AbilityAcceptedMessageType)
    {
        var acceptedMessage = inboundEnvelope.Payload.Deserialize<AbilityAcceptedMessage>(serializerOptions);

        if (acceptedMessage is not null)
        {
            lastAcceptedActivationInstanceId = acceptedMessage.ActivationInstanceId;
            Console.WriteLine($"Activation instance: {acceptedMessage.ActivationInstanceId}");
        }
    }

    if (inboundEnvelope.MessageType == AuthorityTransportProtocol.AbilityCommittedMessageType)
    {
        var committedMessage = inboundEnvelope.Payload.Deserialize<AbilityCommittedMessage>(serializerOptions);

        if (committedMessage is not null)
        {
            Console.WriteLine($"Committed activation instance: {committedMessage.ActivationInstanceId} at {committedMessage.CommittedAtUtc:O}");
        }
    }
}

void EnsureCommittedActivationMatches(ClientInboundEnvelope? inboundEnvelope)
{
    if (string.IsNullOrWhiteSpace(lastAcceptedActivationInstanceId))
    {
        throw new InvalidOperationException("No accepted activation instance was observed before waiting for commit.");
    }

    if (inboundEnvelope?.MessageType != AuthorityTransportProtocol.AbilityCommittedMessageType)
    {
        throw new InvalidOperationException(
            $"Expected '{AuthorityTransportProtocol.AbilityCommittedMessageType}' but received '{inboundEnvelope?.MessageType ?? "unknown"}'.");
    }

    var committedMessage = inboundEnvelope.Payload.Deserialize<AbilityCommittedMessage>(serializerOptions)
        ?? throw new InvalidOperationException("The commit payload was missing or invalid.");

    if (!string.Equals(committedMessage.ActivationInstanceId, lastAcceptedActivationInstanceId, StringComparison.Ordinal))
    {
        throw new InvalidOperationException(
            $"Expected commit for activation '{lastAcceptedActivationInstanceId}' but received '{committedMessage.ActivationInstanceId}'.");
    }
}

internal sealed record TestClientOptions(string WebSocketUrl, string AbilityId, bool ActivateBeforeRegister)
{
    public static TestClientOptions Parse(string[] args)
    {
        var webSocketUrl = "ws://localhost:5274/transport/ws";
        var abilityId = "ability.basic";
        var activateBeforeRegister = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--url":
                    webSocketUrl = ReadValue(args, ++index, "--url");
                    break;

                case "--ability":
                    abilityId = ReadValue(args, ++index, "--ability");
                    break;

                case "--activate-before-register":
                    activateBeforeRegister = true;
                    break;

                default:
                    throw new ArgumentException($"Unknown argument '{args[index]}'.");
            }
        }

        return new TestClientOptions(webSocketUrl, abilityId, activateBeforeRegister);
    }

    private static string ReadValue(string[] args, int index, string optionName)
    {
        if (index >= args.Length)
        {
            throw new ArgumentException($"Expected a value after '{optionName}'.");
        }

        return args[index];
    }
}

internal sealed record ClientInboundEnvelope(
    string MessageType,
    int Version,
    DateTimeOffset ServerUtcNow,
    string? RequestId,
    JsonElement Payload);
