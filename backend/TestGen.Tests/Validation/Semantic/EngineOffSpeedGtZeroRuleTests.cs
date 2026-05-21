using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class EngineOffSpeedGtZeroRuleTests
{
    private static readonly EngineOffSpeedGtZeroRule Rule       = new();
    private static readonly Profile                  AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_EngineOffSpeedAboveZero_ReturnsError()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.engineState",     "OFF")
            .WithParameter("initialConditions.vehicleSpeedMps", 5.0)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(EngineOffSpeedGtZeroRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("initialConditions.vehicleSpeedMps");
        issues[0].Message.Should().Contain("5");
    }

    [Fact]
    public void Check_EngineOffSpeedZero_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.engineState",     "OFF")
            .WithParameter("initialConditions.vehicleSpeedMps", 0.0)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_EngineRunningSpeedPositive_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.engineState",     "RUNNING")
            .WithParameter("initialConditions.vehicleSpeedMps", 30.0)
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
