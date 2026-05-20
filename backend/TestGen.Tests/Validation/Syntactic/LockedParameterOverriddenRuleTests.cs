using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Syntactic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Syntactic;

public sealed class LockedParameterOverriddenRuleTests
{
    private static readonly LockedParameterOverriddenRule Rule = new();

    [Fact]
    public void Check_LockedParameterChanged_ReturnsWarning()
    {
        // Arrange
        var profile = new ProfileBuilder()
            .WithId("cold-start")
            .WithDefault("initialConditions.engineState", "OFF")
            .WithLocked("initialConditions.engineState")
            .Build();
        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.engineState", "RUNNING")
            .Build();

        // Act
        var issues = Rule.Check(scenario, profile).ToList();

        // Assert
        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(LockedParameterOverriddenRule.RuleCode);
        issues[0].Severity.Should().Be("warning");
        issues[0].Field.Should().Be("initialConditions.engineState");
        issues[0].Message.Should().Contain("cold-start");
    }

    [Fact]
    public void Check_LockedParameterUnchanged_ReturnsNoIssues()
    {
        // Arrange
        var profile = new ProfileBuilder()
            .WithDefault("initialConditions.engineState", "OFF")
            .WithLocked("initialConditions.engineState")
            .Build();
        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.engineState", "OFF")
            .Build();

        // Act
        var issues = Rule.Check(scenario, profile);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_LockedKeyAbsentFromScenario_ReturnsNoIssues()
    {
        // Arrange
        var profile = new ProfileBuilder()
            .WithDefault("initialConditions.engineState", "OFF")
            .WithLocked("initialConditions.engineState")
            .Build();
        var scenario = new ScenarioBuilder().Build();

        // Act
        var issues = Rule.Check(scenario, profile);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_NoLockedParameters_ReturnsNoIssues()
    {
        // Arrange
        var profile  = new ProfileBuilder().WithDefault("x", "OFF").Build();
        var scenario = new ScenarioBuilder().WithParameter("x", "ON").Build();

        // Act
        var issues = Rule.Check(scenario, profile);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_LockedKeyHasNoDefaultInProfile_ReturnsNoIssues()
    {
        // Arrange — locked key listed but not present in profile's DefaultParameters
        var profile = new Profile
        {
            ProfileId         = "p",
            DefaultParameters = new Dictionary<string, object>(),
            LockedParameters  = new[] { "initialConditions.engineState" }
        };
        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.engineState", "RUNNING")
            .Build();

        // Act
        var issues = Rule.Check(scenario, profile);

        // Assert
        issues.Should().BeEmpty();
    }
}
