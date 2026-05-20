using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class TimingOverflowRuleTests
{
    private static readonly TimingOverflowRule Rule       = new();
    private static readonly Profile            AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_FaultEndExceedsTestDuration_ReturnsError()
    {
        // Arrange — start=800, duration=300, end=1100 > testDuration=1000
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs",        1000.0)
            .WithParameter("faults[0].startTimeMs", 800.0)
            .WithParameter("faults[0].durationMs",  300.0)
            .Build();

        // Act
        var issues = Rule.Check(scenario, AnyProfile).ToList();

        // Assert
        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(TimingOverflowRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("faults[0].durationMs");
        issues[0].Message.Should().Contain("1100").And.Contain("1000");
    }

    [Fact]
    public void Check_FaultEndExactlyAtTestDuration_ReturnsNoIssues()
    {
        // Arrange — start=500, duration=500, end=1000 == testDuration=1000 (not strictly over)
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs",        1000.0)
            .WithParameter("faults[0].startTimeMs", 500.0)
            .WithParameter("faults[0].durationMs",  500.0)
            .Build();

        // Act
        var issues = Rule.Check(scenario, AnyProfile);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_FaultWithinTestDuration_ReturnsNoIssues()
    {
        // Arrange — start=200, duration=300, end=500 < testDuration=1000
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs",        1000.0)
            .WithParameter("faults[0].startTimeMs", 200.0)
            .WithParameter("faults[0].durationMs",  300.0)
            .Build();

        // Act
        var issues = Rule.Check(scenario, AnyProfile);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_TestDurationMissing_ReturnsNoIssues()
    {
        // Arrange
        var scenario = new ScenarioBuilder()
            .WithParameter("faults[0].startTimeMs", 900.0)
            .WithParameter("faults[0].durationMs",  200.0)
            .Build();

        // Act
        var issues = Rule.Check(scenario, AnyProfile);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_NoFaultKeys_ReturnsNoIssues()
    {
        // Arrange
        var scenario = new ScenarioBuilder()
            .WithParameter("testDurationMs", 1000.0)
            .Build();

        // Act
        var issues = Rule.Check(scenario, AnyProfile);

        // Assert
        issues.Should().BeEmpty();
    }
}
