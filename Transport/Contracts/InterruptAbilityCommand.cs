namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record InterruptAbilityCommand(string? ActivationInstanceId, string? InterruptionCode);
