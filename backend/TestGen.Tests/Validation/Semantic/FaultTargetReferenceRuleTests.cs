using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class FaultTargetReferenceRuleTests
{
    private static readonly FaultTargetReferenceRule Rule       = new();
    private static readonly Profile                  AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_TargetMatchesSignal_NoIssue()
    {
        var s = new ScenarioBuilder()
            .WithParameter("signalList[0].name", "wheel_speed")
            .WithParameter("faults[0].target",   "wheel_speed")
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_TargetMatchesMessageId_NoIssue()
    {
        var s = new ScenarioBuilder()
            .WithParameter("messageDefs[0].id",   "MSG_BRAKE")
            .WithParameter("messageDefs[0].name", "Brake")
            .WithParameter("faults[0].target",    "MSG_BRAKE")
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_TargetUnknown_Error()
    {
        var s = new ScenarioBuilder()
            .WithParameter("signalList[0].name", "wheel_speed")
            .WithParameter("faults[0].target",   "MSG_DOES_NOT_EXIST")
            .Build();

        var issues = Rule.Check(s, AnyProfile).ToList();
        issues.Should().ContainSingle();
        issues[0].Severity.Should().Be("error");
        issues[0].Code.Should().Be(FaultTargetReferenceRule.RuleCode);
    }

    [Fact]
    public void Check_NoSignalsOrMessages_Skipped()
    {
        var s = new ScenarioBuilder()
            .WithParameter("faults[0].target", "wheel_speed")
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }
}
