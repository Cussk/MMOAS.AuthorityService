namespace MMOAS.AuthorityService.Transport.Contracts;

public sealed record AbilityRejectedMessage(string Code, string Message, string? AbilityId);
