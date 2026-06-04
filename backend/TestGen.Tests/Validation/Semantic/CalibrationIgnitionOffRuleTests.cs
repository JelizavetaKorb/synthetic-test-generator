using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class CalibrationIgnitionOffRuleTests
{
    private static readonly CalibrationIgnitionOffRule Rule       = new();
    private static readonly Profile                    AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_CalibrationWithIgnitionOff_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuMode", "CALIBRATION")
            .WithParameter("initialConditions.ignitionKey", "OFF")
            .WithParameter("targetExecution", "HIL")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(CalibrationIgnitionOffRule.RuleCode);
        issues[0].Severity.Should().Be("error");
    }

    [Fact]
    public void Check_CalibrationWithIgnitionOff_BenchMode_ReturnsWarning()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuMode", "CALIBRATION")
            .WithParameter("initialConditions.ignitionKey", "OFF")
            .WithParameter("targetExecution", "bench")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Severity.Should().Be("warning");
    }

    [Fact]
    public void Check_CalibrationWithIgnitionOn_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuMode", "CALIBRATION")
            .WithParameter("initialConditions.ignitionKey", "ON")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_MissingEcuMode_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.ignitionKey", "OFF")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }
}
