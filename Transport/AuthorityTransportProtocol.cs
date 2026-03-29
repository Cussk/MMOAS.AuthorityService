namespace MMOAS.AuthorityService.Transport;

public static class AuthorityTransportProtocol
{
    public const int Version = 1;

    public const string HelloMessageType = "transport.hello";
    public const string ReadyMessageType = "transport.ready";
    public const string RegisterEntityMessageType = "transport.register-entity";
    public const string EntityRegisteredMessageType = "transport.entity-registered";
    public const string ErrorMessageType = "transport.error";
}
