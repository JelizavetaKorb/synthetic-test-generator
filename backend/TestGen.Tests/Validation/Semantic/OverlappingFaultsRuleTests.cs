using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class OverlappingFaultsRuleTests
{
    private static readonly OverlappingFaultsRule Rule       = new();
    private static readonly Profile               AnyProfile = new ProfileBuilder().Build();

    private static Scenario TwoFaults(string type0, double start0, double dur0,
                                      string type1, double start1, double dur1, string target1 = "rpm") =>
        new ScenarioBuilder()
            .WithParameter("faults[0].target",      "rpm")
            .WithParameter("faults[0].type",        type0)
            .WithParameter("faults[0].startTimeMs", start0)
            .WithParameter("faults[0].durationMs",  dur0)
            .WithParameter("faults[1].target",      target1)
            .WithParameter("faults[1].type",        type1)
            .WithParameter("faults[1].startTimeMs", start1)
            .WithParameter("faults[1].durationMs",  dur1)
            .Build();

    [Fact]
    public void Check_DifferentTypesOverlapOnSameTarget_ReturnsError()
    {
        var issues = Rule.Check(TwoFaults("bit_flip", 0, 500, "message_loss", 200, 500), AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(OverlappingFaultsRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("faults[1].startTimeMs");
    }

    [Fact]
    public void Check_BackToBackFaults_ReturnsNoIssues() =>
        Rule.Check(TwoFaults("bit_flip", 0, 100, "message_loss", 100, 100), AnyProfile).Should().BeEmpty();

    [Fact]
    public void Check_SameTypeOverlap_ReturnsNoIssues() =>
        Rule.Check(TwoFaults("bit_flip", 0, 500, "bit_flip", 200, 500), AnyProfile).Should().BeEmpty();

    [Fact]
    public void Check_DifferentTargets_ReturnsNoIssues() =>
        Rule.Check(TwoFaults("bit_flip", 0, 500, "message_loss", 200, 500, target1: "speed"), AnyProfile)
            .Should().BeEmpty();

    [Fact]
    public void Check_ProfileArray_IsChecked()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("faults", JsonDocument.Parse("""
                [{"target":"rpm","type":"bit_flip","startTimeMs":0,"durationMs":500},
                 {"target":"rpm","type":"message_loss","startTimeMs":100,"durationMs":500}]
                """).RootElement)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().ContainSingle();
    }
}
