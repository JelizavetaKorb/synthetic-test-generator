using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class GearParkSpeedGtZeroRuleTests
{
    private static readonly GearParkSpeedGtZeroRule Rule       = new();
    private static readonly Profile                 AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_GearParkSpeedAboveZero_ReturnsError()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.gearPosition",    "P")
            .WithParameter("initialConditions.vehicleSpeedMps", 3.0)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(GearParkSpeedGtZeroRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("initialConditions.vehicleSpeedMps");
    }

    [Fact]
    public void Check_GearParkSpeedZero_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.gearPosition",    "P")
            .WithParameter("initialConditions.vehicleSpeedMps", 0.0)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_GearDriveSpeedPositive_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.gearPosition",    "D")
            .WithParameter("initialConditions.vehicleSpeedMps", 50.0)
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
