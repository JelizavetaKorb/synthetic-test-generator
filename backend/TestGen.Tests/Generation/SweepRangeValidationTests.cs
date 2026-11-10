using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TestGen.Core.Generation;
using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Core.Validation;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Generation;

public sealed class SweepRangeValidationTests
{
    private static GenerateRequest Request(string strategy, SweepSpec spec) => new()
    {
        ProfileId       = "p",
        Strategy        = strategy,
        SweepParameters = new Dictionary<string, SweepSpec> { ["x"] = spec }
    };

    private static IGenerationService Generator(string strategy)
    {
        var pipeline = new GeneratorPipeline(
            new FakeProfileService(new ProfileBuilder().WithId("p").Build()), new PassThrough());
        return strategy == "pairwise"
            ? new PairwiseScenarioGenerator(pipeline, NullLogger<PairwiseScenarioGenerator>.Instance)
            : new CartesianScenarioGenerator(pipeline, NullLogger<CartesianScenarioGenerator>.Instance);
    }

    public static IEnumerable<object[]> InvalidRanges() => new[]
    {
        new object[] { "cartesian", new SweepSpec { Type = "linear_range",    From = 10, To = 0,  Step = 1  } },
        new object[] { "pairwise",  new SweepSpec { Type = "linear_range",    From = 10, To = 0,  Step = 1  } },
        new object[] { "cartesian", new SweepSpec { Type = "geometric_range", From = 8,  To = 1,  Step = 2  } },
        new object[] { "cartesian", new SweepSpec { Type = "linear_range",    From = 0,  To = 10, Step = 0  } },
        new object[] { "cartesian", new SweepSpec { Type = "linear_range",    From = 0,  To = 10, Step = -1 } },
    };

    [Theory(Timeout = 3000)]
    [MemberData(nameof(InvalidRanges))]
    public async Task InvalidRange_ReturnsReadableError(string strategy, SweepSpec spec)
    {
        var result = await Task.Run(() => Generator(strategy).Generate(Request(strategy, spec)));

        result.Scenarios.Should().BeEmpty();
        result.Issues.Should().ContainSingle(i =>
            i.Code == "INVALID_SWEEP_SPEC" && i.Severity == "error" && i.Field == "x");
    }

    [Fact]
    public void SingleValueRange_IsValid()
    {
        var result = Generator("cartesian").Generate(
            Request("cartesian", new SweepSpec { Type = "linear_range", From = 5, To = 5, Step = 1 }));

        result.Issues.Should().BeEmpty();
        result.Scenarios.Should().ContainSingle();
    }

    private sealed class PassThrough : IScenarioValidator
    {
        public ValidationResult Validate(Scenario s, Profile p) => new() { IsValid = true };
    }
}
