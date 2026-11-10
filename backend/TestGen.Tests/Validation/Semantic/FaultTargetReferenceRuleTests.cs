using System.Text.Json;
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

    [Fact]
    public void Check_FaultFromProfileArray_UnknownTarget_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("signalList", JsonDocument.Parse("""[{"name":"engine_status"}]""").RootElement)
            .WithParameter("faults",     JsonDocument.Parse("""[{"target":"MSG_ENGINE_STATUS"}]""").RootElement)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().ContainSingle(i => i.Field == "faults[0].target");
    }

    [Fact]
    public void Check_FaultFromProfileArray_TargetsDeclaredMessage_NoIssue()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("messageDefs", JsonDocument.Parse("""[{"id":"MSG_ENGINE_STATUS","name":"ENGINE_STATUS"}]""").RootElement)
            .WithParameter("faults",      JsonDocument.Parse("""[{"target":"MSG_ENGINE_STATUS"}]""").RootElement)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }
}
