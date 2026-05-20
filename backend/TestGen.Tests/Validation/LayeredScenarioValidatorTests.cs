using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TestGen.Core.Models;
using TestGen.Core.Validation;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation;

public sealed class LayeredScenarioValidatorTests
{
    private static LayeredScenarioValidator Build(params IValidationRule[] rules) =>
        new(rules, NullLogger<LayeredScenarioValidator>.Instance);

    private static readonly Profile AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Validate_NoRules_ReturnsValid()
    {
        // Arrange
        var validator = Build();
        var scenario  = new ScenarioBuilder().Build();

        // Act
        var result = validator.Validate(scenario, AnyProfile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Validate_OneErrorRule_ReturnsInvalid()
    {
        // Arrange
        var validator = Build(new FixedIssueRule("ERR", "error"));
        var scenario  = new ScenarioBuilder().Build();

        // Act
        var result = validator.Validate(scenario, AnyProfile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Issues.Should().ContainSingle(i => i.Severity == "error" && i.Code == "ERR");
    }

    [Fact]
    public void Validate_WarningOnly_ReturnsValidWithWarning()
    {
        // Arrange
        var validator = Build(new FixedIssueRule("WARN", "warning"));
        var scenario  = new ScenarioBuilder().Build();

        // Act
        var result = validator.Validate(scenario, AnyProfile);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Issues.Should().ContainSingle(i => i.Severity == "warning" && i.Code == "WARN");
    }

    [Fact]
    public void Validate_MultipleRules_AllIssuesCollected()
    {
        // Arrange — two rules, each firing one issue; validator must not short-circuit
        var validator = Build(new FixedIssueRule("W1", "warning"), new FixedIssueRule("E1", "error"));
        var scenario  = new ScenarioBuilder().Build();

        // Act
        var result = validator.Validate(scenario, AnyProfile);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Issues.Should().HaveCount(2);
        result.Issues[0].Code.Should().Be("W1");
        result.Issues[1].Code.Should().Be("E1");
    }

    private sealed class FixedIssueRule : IValidationRule
    {
        private readonly ValidationIssue _issue;

        public FixedIssueRule(string code, string severity) =>
            _issue = new ValidationIssue { Severity = severity, Code = code, Field = "", Message = "" };

        public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
        {
            yield return _issue;
        }
    }
}
