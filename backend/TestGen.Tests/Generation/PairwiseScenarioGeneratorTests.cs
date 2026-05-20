using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TestGen.Core.Generation;
using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Core.Validation;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Generation;

public sealed class PairwiseScenarioGeneratorTests
{
    private static PairwiseScenarioGenerator BuildSut(Profile profile)
    {
        var pipeline = new GeneratorPipeline(
            new FakeProfileService(profile),
            new PassThroughValidator());
        return new PairwiseScenarioGenerator(pipeline, NullLogger<PairwiseScenarioGenerator>.Instance);
    }

    private static GenerateRequest RequestFor(string id = "p") => new()
    {
        ProfileId = id,
        Strategy  = "pairwise"
    };

    private static GenerateRequest ThreeSweepRequest(string id = "p") => new()
    {
        ProfileId       = id,
        Strategy        = "pairwise",
        SweepParameters = new Dictionary<string, SweepSpec>
        {
            ["x"] = new SweepSpec { Type = "linear_range", From = 1, To = 3, Step = 1 },
            ["y"] = new SweepSpec { Type = "linear_range", From = 1, To = 3, Step = 1 },
            ["z"] = new SweepSpec { Type = "linear_range", From = 1, To = 3, Step = 1 }
        }
    };

    private static double D(Scenario s, string key) => Convert.ToDouble(s.Parameters[key]);

    [Fact]
    public void Generate_NoSweeps_ReturnsOneScenario()
    {
        // Arrange
        var profile = new ProfileBuilder().WithId("p").Build();

        // Act
        var result = BuildSut(profile).Generate(RequestFor());

        // Assert
        result.Scenarios.Should().ContainSingle();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Generate_SingleSweep_SameAsCartesian()
    {
        // Arrange — one sweep with 4 values; pairwise == cartesian for a single axis
        var profile = new ProfileBuilder().WithId("p").Build();
        var request = RequestFor();
        request.SweepParameters = new Dictionary<string, SweepSpec>
        {
            ["x"] = new SweepSpec { Type = "linear_range", From = 1, To = 4, Step = 1 }
        };

        // Act
        var result = BuildSut(profile).Generate(request);

        // Assert
        result.Scenarios.Should().HaveCount(4);
    }

    [Fact]
    public void Generate_ThreeSweeps_FewerThanCartesian()
    {
        // Arrange — 3×3×3 = 27 cartesian scenarios; pairwise must need fewer
        var profile = new ProfileBuilder().WithId("p").Build();

        // Act
        var result = BuildSut(profile).Generate(ThreeSweepRequest());

        // Assert
        result.Scenarios.Count.Should().BeLessThan(27,
            "pairwise should need fewer than 3×3×3=27 cartesian scenarios");
    }

    [Fact]
    public void Generate_ThreeSweeps_AllPairsCovered()
    {
        // Arrange — every (value_i, value_j) pair across all axis combinations must appear
        var profile = new ProfileBuilder().WithId("p").Build();

        // Act
        var result = BuildSut(profile).Generate(ThreeSweepRequest());

        // Assert
        double[] v  = [1, 2, 3];
        var      ss = result.Scenarios;
        foreach (var a in v)
            foreach (var b in v)
            {
                ss.Should().Contain(s => D(s,"x")==a && D(s,"y")==b, $"pair x={a},y={b} missing");
                ss.Should().Contain(s => D(s,"x")==a && D(s,"z")==b, $"pair x={a},z={b} missing");
                ss.Should().Contain(s => D(s,"y")==a && D(s,"z")==b, $"pair y={a},z={b} missing");
            }
    }

    [Fact]
    public void Generate_Deterministic_SameInputSameOutput()
    {
        // Arrange
        var profile = new ProfileBuilder().WithId("p").Build();
        var sut     = BuildSut(profile);

        // Act
        var r1 = sut.Generate(ThreeSweepRequest());
        var r2 = sut.Generate(ThreeSweepRequest());

        // Assert
        r1.Scenarios.Count.Should().Be(r2.Scenarios.Count);
        for (var i = 0; i < r1.Scenarios.Count; i++)
            foreach (var k in r1.Scenarios[i].Parameters.Keys)
                r1.Scenarios[i].Parameters[k].ToString()
                    .Should().Be(r2.Scenarios[i].Parameters[k].ToString(),
                        $"scenario {i} key {k} must match across identical runs");
    }

    private sealed class PassThroughValidator : IScenarioValidator
    {
        public ValidationResult Validate(Scenario s, Profile p) => new() { IsValid = true };
    }
}
