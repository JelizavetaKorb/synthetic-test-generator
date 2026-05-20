using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TestGen.Core.Generation;
using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Core.Validation;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Generation;

public sealed class CartesianScenarioGeneratorTests
{
    private static CartesianScenarioGenerator BuildSut(
        Profile?            profile,
        IScenarioValidator? validator = null)
    {
        var service  = profile is null ? new FakeProfileService() : new FakeProfileService(profile);
        var pipeline = new GeneratorPipeline(service, validator ?? new PassThroughValidator());
        return new CartesianScenarioGenerator(pipeline, NullLogger<CartesianScenarioGenerator>.Instance);
    }

    [Fact]
    public void Generate_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        // Arrange
        var sut     = BuildSut(profile: null);
        var request = new GenerateRequest { ProfileId = "missing", Strategy = "cartesian" };

        // Act
        var result = sut.Generate(request);

        // Assert
        result.Scenarios.Should().BeEmpty();
        result.Issues.Should().ContainSingle(i =>
            i.Code == "PROFILE_NOT_FOUND" && i.Severity == "error");
    }

    [Fact]
    public void Generate_NoSweepsNoOverrides_ReturnsOneScenarioWithDefaults()
    {
        // Arrange
        var profile = new ProfileBuilder()
            .WithId("p")
            .WithDefault("a", 1)
            .WithDefault("b", "hello")
            .WithDefault("c", true)
            .Build();
        var request = new GenerateRequest { ProfileId = "p", Strategy = "cartesian" };

        // Act
        var result = BuildSut(profile).Generate(request);

        // Assert
        result.Scenarios.Should().ContainSingle();
        var s = result.Scenarios[0];
        s.Parameters.Should().ContainKey("a").WhoseValue.Should().Be(1);
        s.Parameters.Should().ContainKey("b").WhoseValue.Should().Be("hello");
        s.Parameters.Should().ContainKey("c").WhoseValue.Should().Be(true);
    }

    [Fact]
    public void Generate_OverrideApplied_ScenarioReflectsOverride()
    {
        // Arrange
        var profile = new ProfileBuilder().WithId("p").WithDefault("x", 1).Build();
        var request = new GenerateRequest
        {
            ProfileId = "p",
            Strategy  = "cartesian",
            Overrides = new Dictionary<string, object> { ["x"] = 99 }
        };

        // Act
        var result = BuildSut(profile).Generate(request);

        // Assert
        result.Scenarios.Should().ContainSingle();
        result.Scenarios[0].Parameters["x"].Should().Be(99);
    }

    [Fact]
    public void Generate_OneSweepLinearRange_CorrectCount()
    {
        // Arrange — sweep from 0 to 2 step 1 produces values [0, 1, 2]
        var profile = new ProfileBuilder().WithId("p").Build();
        var request = new GenerateRequest
        {
            ProfileId        = "p",
            Strategy         = "cartesian",
            SweepParameters  = new Dictionary<string, SweepSpec>
            {
                ["v"] = new SweepSpec { Type = "linear_range", From = 0, To = 2, Step = 1 }
            }
        };

        // Act
        var result = BuildSut(profile).Generate(request);

        // Assert
        result.Scenarios.Should().HaveCount(3);
    }

    [Fact]
    public void Generate_TwoSweeps_CartesianProduct()
    {
        // Arrange — sweep "a"=[1,2], "b"=[10,20,30]; cartesian = 6 combos
        var profile = new ProfileBuilder().WithId("p").Build();
        var request = new GenerateRequest
        {
            ProfileId       = "p",
            Strategy        = "cartesian",
            SweepParameters = new Dictionary<string, SweepSpec>
            {
                ["a"] = new SweepSpec { Type = "linear_range", From = 1,  To = 2,  Step = 1  },
                ["b"] = new SweepSpec { Type = "linear_range", From = 10, To = 30, Step = 10 }
            }
        };

        // Act
        var result = BuildSut(profile).Generate(request);

        // Assert
        result.Scenarios.Should().HaveCount(6);
        result.Scenarios.Should().Contain(s => D(s,"a") == 1.0 && D(s,"b") == 30.0,
            "spot-check: a=1, b=30 must appear");
        result.Scenarios.Should().Contain(s => D(s,"a") == 2.0 && D(s,"b") == 10.0,
            "spot-check: a=2, b=10 must appear");
    }

    [Fact]
    public void Generate_InvalidScenarioFilteredByValidator_DroppedFromOutput()
    {
        // Arrange — sweep speed 0..2; only speed=0 is valid with engineState=OFF
        var profile = new ProfileBuilder()
            .WithId("p")
            .WithDefault("initialConditions.engineState",     "OFF")
            .WithDefault("initialConditions.vehicleSpeedMps", 0.0)
            .Build();
        var request = new GenerateRequest
        {
            ProfileId       = "p",
            Strategy        = "cartesian",
            SweepParameters = new Dictionary<string, SweepSpec>
            {
                ["initialConditions.vehicleSpeedMps"] =
                    new SweepSpec { Type = "linear_range", From = 0, To = 2, Step = 1 }
            }
        };
        var sut = BuildSut(profile, new SingleRuleValidator(new EngineOffSpeedGtZeroRule()));

        // Act
        var result = sut.Generate(request);

        // Assert
        result.Scenarios.Should().ContainSingle(s => D(s, "initialConditions.vehicleSpeedMps") == 0.0);
        result.Issues.Where(i => i.Code == EngineOffSpeedGtZeroRule.RuleCode)
                     .Should().HaveCount(2, "scenarios with speed=1 and speed=2 must be dropped");
    }

    private static double D(Scenario s, string key) => Convert.ToDouble(s.Parameters[key]);

    private sealed class PassThroughValidator : IScenarioValidator
    {
        public ValidationResult Validate(Scenario scenario, Profile profile) =>
            new() { IsValid = true };
    }

    private sealed class SingleRuleValidator : IScenarioValidator
    {
        private readonly IValidationRule _rule;
        public SingleRuleValidator(IValidationRule rule) => _rule = rule;

        public ValidationResult Validate(Scenario scenario, Profile profile)
        {
            var issues    = _rule.Check(scenario, profile).ToList();
            var hasErrors = issues.Any(i => string.Equals(i.Severity, "error",
                                StringComparison.OrdinalIgnoreCase));
            return new ValidationResult { IsValid = !hasErrors, Issues = issues };
        }
    }
}
