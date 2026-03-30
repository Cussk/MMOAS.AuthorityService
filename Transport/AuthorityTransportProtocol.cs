namespace MMOAS.AuthorityService.Transport;

public static class AuthorityTransportProtocol
{
    public const int Version = 1;

    public const string HelloMessageType = "transport.hello";
    public const string ReadyMessageType = "transport.ready";
    public const string RegisterEntityMessageType = "transport.register-entity";
    public const string EntityRegisteredMessageType = "transport.entity-registered";
    public const string ActivateAbilityMessageType = "transport.activate-ability";
    public const string InterruptAbilityMessageType = "transport.interrupt-ability";
    public const string AbilityAcceptedMessageType = "transport.ability-accepted";
    public const string AbilityInterruptedMessageType = "transport.ability-interrupted";
    public const string AbilityCommittedMessageType = "transport.ability-committed";
    public const string AbilityRejectedMessageType = "transport.ability-rejected";
    public const string ErrorMessageType = "transport.error";
}
