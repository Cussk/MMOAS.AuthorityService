using MMOAS.AuthorityService.Domain.Abilities;

namespace MMOAS.AuthorityService.Tests.Domain.Abilities;

public sealed class AbilityActivationValidatorTests
{
    private readonly AbilityActivationValidator _validator = new();

    [Fact]
    public void Evaluate_RejectsUnknownSession()
    {
        var decision = _validator.Evaluate(new AbilityActivationContext(
            "session-001",
            false,
            false,
            null,
            "ability.basic"));

        Assert.False(decision.Accepted);
        Assert.Equal("activation.session-not-found", decision.Code);
    }

    [Fact]
    public void Evaluate_RejectsSessionWithoutHello()
    {
        var decision = _validator.Evaluate(new AbilityActivationContext(
            "session-001",
            true,
            false,
            null,
            "ability.basic"));

        Assert.False(decision.Accepted);
        Assert.Equal("activation.hello-required", decision.Code);
    }

    [Fact]
    public void Evaluate_RejectsSessionWithoutRegisteredEntity()
    {
        var decision = _validator.Evaluate(new AbilityActivationContext(
            "session-001",
            true,
            true,
            null,
            "ability.basic"));

        Assert.False(decision.Accepted);
        Assert.Equal("activation.entity-required", decision.Code);
    }

    [Fact]
    public void Evaluate_RejectsEmptyAbilityId()
    {
        var decision = _validator.Evaluate(new AbilityActivationContext(
            "session-001",
            true,
            true,
            "entity-001",
            string.Empty));

        Assert.False(decision.Accepted);
        Assert.Equal("activation.invalid-ability", decision.Code);
    }

    [Fact]
    public void Evaluate_AcceptsReadySessionWithAbilityId()
    {
        var decision = _validator.Evaluate(new AbilityActivationContext(
            "session-001",
            true,
            true,
            "entity-001",
            "ability.basic"));

        Assert.True(decision.Accepted);
        Assert.Equal("entity-001", decision.EntityId);
        Assert.Null(decision.Code);
    }
}
