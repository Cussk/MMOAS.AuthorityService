using MMOAS.AuthorityService.Application.Abilities;
using MMOAS.AuthorityService.Domain.Abilities;
using MMOAS.AuthorityService.State;

namespace MMOAS.AuthorityService.Tests.Application.Abilities;

public sealed class AbilityActivationServiceTests
{
    [Fact]
    public async Task ActivateAsync_RejectsUnknownSession()
    {
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(sessionStore, validator);

        var result = await service.ActivateAsync("session-001", "ability.basic", CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Equal("activation.session-not-found", result.Code);
    }

    [Fact]
    public async Task ActivateAsync_TrimsAbilityIdBeforeValidation()
    {
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(sessionStore, validator);

        sessionStore.CreateOrGet("session-001", new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        sessionStore.MarkHelloCompleted("session-001");
        sessionStore.AssignRegisteredEntity("session-001", "entity-001");

        var result = await service.ActivateAsync("session-001", " ability.basic ", CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal("ability.basic", result.AbilityId);
    }

    [Fact]
    public async Task ActivateAsync_RejectsWhitespaceOnlyAbilityId()
    {
        var sessionStore = new InMemoryAuthoritySessionStore();
        var validator = new AbilityActivationValidator();
        var service = new AbilityActivationService(sessionStore, validator);

        sessionStore.CreateOrGet("session-001", new DateTimeOffset(2026, 03, 29, 12, 00, 00, TimeSpan.Zero));
        sessionStore.MarkHelloCompleted("session-001");
        sessionStore.AssignRegisteredEntity("session-001", "entity-001");

        var result = await service.ActivateAsync("session-001", "   ", CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.Equal("activation.invalid-ability", result.Code);
    }
}
