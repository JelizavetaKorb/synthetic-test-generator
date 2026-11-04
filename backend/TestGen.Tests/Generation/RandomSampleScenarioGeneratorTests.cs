using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TestGen.Core.Generation;
using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Core.Validation;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Generation;

public sealed class RandomSampleScenarioGeneratorTests
{
    private static RandomSampleScenarioGenerator BuildSut(Profile profile)
    {
        var pipeline = new GeneratorPipeline(new FakeProfileService(profile), new PassThroughValidator());
        return new RandomSampleScenarioGenerator(pipeline, NullLogger<RandomSampleScenarioGenerator>.Instance);
    }

    private static GenerateRequest Request(int maxScenarios) => new()
    {
        ProfileId       = "p",
        Strategy        = "random_sample",
        MaxScenarios    = maxScenarios,
        SweepParameters = new Dictionary<string, SweepSpec>
        {
            ["x"] = new SweepSpec { Type = "linear_range", From = 1, To = 10, Step = 1 },
            ["y"] = new SweepSpec { Type = "linear_range", From = 1, To = 10, Step = 1 }
        }
    };

    private static string Key(Scenario s) => $"{s.Parameters["x"]}|{s.Parameters["y"]}";

    [Fact]
    public void Generate_ProductLargerThanMax_ReturnsExactlyMaxDistinctScenarios()
    {
        var profile = new ProfileBuilder().WithId("p").Build();

        var result = BuildSut(profile).Generate(Request(15));

        result.Issues.Should().BeEmpty();
        result.Scenarios.Should().HaveCount(15);
        result.Scenarios.Select(Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Generate_ProductSmallerThanMax_ReturnsWholeProduct()
    {
        var profile = new ProfileBuilder().WithId("p").Build();

        var result = BuildSut(profile).Generate(Request(500));

        result.Scenarios.Should().HaveCount(100);
    }

    [Fact]
    public void Generate_SameRequestTwice_IsDeterministic()
    {
        var profile = new ProfileBuilder().WithId("p").Build();
        var sut     = BuildSut(profile);

        var first  = sut.Generate(Request(10)).Scenarios.Select(Key);
        var second = sut.Generate(Request(10)).Scenarios.Select(Key);

        first.Should().Equal(second);
    }

    [Fact]
    public void Generate_NoSweeps_ReturnsOneScenario()
    {
        var profile = new ProfileBuilder().WithId("p").Build();

        var result = BuildSut(profile).Generate(new GenerateRequest { ProfileId = "p", Strategy = "random_sample" });

        result.Scenarios.Should().ContainSingle();
    }

    private sealed class PassThroughValidator : IScenarioValidator
    {
        public ValidationResult Validate(Scenario s, Profile p) => new() { IsValid = true };
    }
}
