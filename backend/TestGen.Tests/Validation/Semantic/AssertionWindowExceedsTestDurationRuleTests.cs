using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class AssertionWindowExceedsTestDurationRuleTests
{
    private static readonly AssertionWindowExceedsTestDurationRule Rule       = new();
    private static readonly Profile                                AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_WindowEndsAfterTest_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs",               1000.0)
            .WithParameter("assertions[0].id",              "A1")
            .WithParameter("assertions[0].timeWindowMs.to", 1500.0)
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(AssertionWindowExceedsTestDurationRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("assertions[0].time_window_ms");
        issues[0].Message.Should().Contain("A1").And.Contain("1500").And.Contain("1000");
    }

    [Theory]
    [InlineData(1000.0)]
    [InlineData(500.0)]
    public void Check_WindowInsideTest_ReturnsNoIssues(double windowEnd)
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs",               1000.0)
            .WithParameter("assertions[0].timeWindowMs.to", windowEnd)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_WindowFromProfileArray_EndsAfterTest_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs", 1000.0)
            .WithParameter("assertions", JsonDocument.Parse("""[{"id":"A1","timeWindowMs":{"from":0,"to":2000}}]""").RootElement)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().ContainSingle();
    }

    [Fact]
    public void Check_NoTestDuration_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("assertions[0].timeWindowMs.to", 5000.0)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }
}
