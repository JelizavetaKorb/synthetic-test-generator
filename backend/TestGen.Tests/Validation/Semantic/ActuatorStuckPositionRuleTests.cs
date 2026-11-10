using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class ActuatorStuckPositionRuleTests
{
    private static readonly ActuatorStuckPositionRule Rule       = new();
    private static readonly Profile                   AnyProfile = new ProfileBuilder().Build();

    private static Scenario StuckAt(double position, string type = "actuator_stuck") => new ScenarioBuilder()
        .WithParameter("signalList[0].name", "throttle")
        .WithParameter("signalList[0].min",  0.0)
        .WithParameter("signalList[0].max",  100.0)
        .WithParameter("faults[0].type",     type)
        .WithParameter("faults[0].target",   "throttle")
        .WithParameter("faults[0].position", position)
        .Build();

    [Fact]
    public void Check_PositionOutsideSignalRange_ReturnsError()
    {
        var issues = Rule.Check(StuckAt(150.0), AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(ActuatorStuckPositionRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("faults[0].position");
        issues[0].Message.Should().Contain("150").And.Contain("throttle");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    public void Check_PositionInsideRange_ReturnsNoIssues(double position) =>
        Rule.Check(StuckAt(position), AnyProfile).Should().BeEmpty();

    [Fact]
    public void Check_OtherFaultType_ReturnsNoIssues() =>
        Rule.Check(StuckAt(150.0, type: "bit_flip"), AnyProfile).Should().BeEmpty();

    [Fact]
    public void Check_ProfileArrays_AreChecked()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("signalList", JsonDocument.Parse("""[{"name":"throttle","min":0,"max":100}]""").RootElement)
            .WithParameter("faults", JsonDocument.Parse("""[{"type":"actuator_stuck","target":"throttle","position":-5}]""").RootElement)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().ContainSingle();
    }

    [Fact]
    public void Check_TargetWithoutDeclaredRange_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("faults[0].type",     "actuator_stuck")
            .WithParameter("faults[0].target",   "throttle")
            .WithParameter("faults[0].position", 150.0)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }
}
