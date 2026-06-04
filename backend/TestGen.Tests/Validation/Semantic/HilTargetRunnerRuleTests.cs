using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class HilTargetRunnerRuleTests
{
    private static readonly HilTargetRunnerRule Rule       = new();
    private static readonly Profile             AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_HilWithHilBench_NoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("targetExecution",        "HIL")
            .WithParameter("execution.targetRunner", "HIL_bench")
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_HilWithWrongRunner_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("targetExecution",        "HIL")
            .WithParameter("execution.targetRunner", "SIL_runner")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();
        issues.Should().ContainSingle();
        issues[0].Severity.Should().Be("error");
        issues[0].Code.Should().Be(HilTargetRunnerRule.RuleCode);
    }

    [Fact]
    public void Check_HilWithMissingRunner_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("targetExecution", "HIL")
            .Build();

        Rule.Check(scenario, AnyProfile).Should().ContainSingle()
            .Which.Severity.Should().Be("error");
    }

    [Fact]
    public void Check_NonHilExecution_NoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("targetExecution",        "simulation")
            .WithParameter("execution.targetRunner", "CARLA")
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }
}
