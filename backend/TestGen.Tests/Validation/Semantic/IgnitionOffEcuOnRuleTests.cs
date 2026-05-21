using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class IgnitionOffEcuOnRuleTests
{
    private static readonly IgnitionOffEcuOnRule Rule       = new();
    private static readonly Profile              AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_IgnitionOffEcuOn_ReturnsWarning()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.ignitionKey", "OFF")
            .WithParameter("ecuPowerState",                 "ON")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(IgnitionOffEcuOnRule.RuleCode);
        issues[0].Severity.Should().Be("warning");
        issues[0].Field.Should().Be("ecuPowerState");
    }

    [Fact]
    public void Check_IgnitionOnEcuOn_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.ignitionKey", "ON")
            .WithParameter("ecuPowerState",                 "ON")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_IgnitionOffEcuOff_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.ignitionKey", "OFF")
            .WithParameter("ecuPowerState",                 "OFF")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_ParametersMissing_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder().Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }
}
