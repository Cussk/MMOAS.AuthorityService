namespace MMOAS.AuthorityService.Application.Entities;

public interface IEntityRegistrationService
{
    ValueTask<RegisteredEntity> RegisterAsync(CancellationToken cancellationToken);
}
