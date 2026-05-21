using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Safety;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Safety;

public sealed class AsilDMissingCriticalAssertionsRuleTests
{
    private static readonly AsilDMissingCriticalAssertionsRule Rule       = new();
    private static readonly Profile                            AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_AsilDNoAssertions_ReturnsError()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel", "D")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(AsilDMissingCriticalAssertionsRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("asilLevel");
    }

    [Fact]
    public void Check_AsilDAssertionNotCritical_ReturnsError()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel",              "D")
            .WithParameter("assertions[0].severity", "high")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(AsilDMissingCriticalAssertionsRule.RuleCode);
    }

    [Fact]
    public void Check_AsilDHasCriticalAssertion_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel",              "D")
            .WithParameter("assertions[0].severity", "critical")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_AsilLevelNotD_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel", "B")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_AsilLevelMissing_ReturnsNoIssues()
    {

        var scenario = new ScenarioBuilder().Build();

        var issues = Rule.Check(scenario, AnyProfile);

        issues.Should().BeEmpty();
    }
}
