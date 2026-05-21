using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class FaultIntensityOutOfRangeRuleTests
{
    private static readonly FaultIntensityOutOfRangeRule Rule       = new();
    private static readonly Profile                      AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_IntensityAboveOne_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("faults[0].faultId", "F1")
            .WithParameter("faults[0].intensity", 1.5)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(FaultIntensityOutOfRangeRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("faults[0].intensity");
    }

    [Fact]
    public void Check_IntensityBelowZero_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("faults[0].faultId", "F1")
            .WithParameter("faults[0].intensity", -0.1)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Severity.Should().Be("error");
    }

    [Fact]
    public void Check_IntensityInRange_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("faults[0].faultId", "F1")
            .WithParameter("faults[0].intensity", 0.5)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_NoFaults_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs", 1000.0)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }
}

