using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Syntactic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Syntactic;

public sealed class RequiredFieldsRuleTests
{
    private static readonly RequiredFieldsRule Rule       = new();
    private static readonly Profile            AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_AllPresent_NoIssue()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .Build();
        // ScenarioBuilder seeds Name = "Test Scenario"
        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_AllMissing_ThreeErrors()
    {
        var s = new Scenario { Name = "", Parameters = new() };

        var issues = Rule.Check(s, AnyProfile).ToList();
        issues.Should().HaveCount(3);
        issues.Should().OnlyContain(i => i.Severity == "error");
        issues.Select(i => i.Field).Should().BeEquivalentTo(new[]
        {
            "name", "ecuId", "targetExecution"
        });
    }
}
