using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Tests.Generation;

public sealed class ScenarioGeneratorRouterTests
{
    private sealed class SpyGenerator : IGenerationService
    {
        public bool   Called { get; private set; }
        public string Label  { get; }
        public SpyGenerator(string label) => Label = label;

        public GenerateResult Generate(GenerateRequest request)
        {
            Called = true;
            return new GenerateResult
            {
                Scenarios = new[] { new Scenario { Parameters = new Dictionary<string, object> { ["label"] = Label } } }
            };
        }
    }

    private static ScenarioGeneratorRouter BuildRouter(out SpyGenerator cartesian, out SpyGenerator pairwise)
    {
        cartesian = new SpyGenerator("cartesian");
        pairwise  = new SpyGenerator("pairwise");
        return new ScenarioGeneratorRouter(cartesian, pairwise);
    }

    [Fact]
    public void Generate_CartesianStrategy_DelegatesToCartesian()
    {

        var router = BuildRouter(out var cartesian, out var pairwise);

        var result = router.Generate(new GenerateRequest { Strategy = "cartesian" });

        cartesian.Called.Should().BeTrue();
        pairwise.Called.Should().BeFalse();
        result.Scenarios[0].Parameters["label"].Should().Be("cartesian");
    }

    [Fact]
    public void Generate_PairwiseStrategy_DelegatesToPairwise()
    {

        var router = BuildRouter(out var cartesian, out var pairwise);

        var result = router.Generate(new GenerateRequest { Strategy = "pairwise" });

        pairwise.Called.Should().BeTrue();
        cartesian.Called.Should().BeFalse();
        result.Scenarios[0].Parameters["label"].Should().Be("pairwise");
    }

    [Fact]
    public void Generate_EmptyStrategy_DefaultsToCartesian()
    {

        var router = BuildRouter(out var cartesian, out _);

        router.Generate(new GenerateRequest { Strategy = string.Empty });

        cartesian.Called.Should().BeTrue();
    }

    [Fact]
    public void Generate_NullStrategy_DefaultsToCartesian()
    {

        var router = BuildRouter(out var cartesian, out _);

        router.Generate(new GenerateRequest { Strategy = null! });

        cartesian.Called.Should().BeTrue();
    }

    [Fact]
    public void Generate_StrategyIsCaseInsensitive_DelegatesToCorrectGenerator()
    {

        var router = BuildRouter(out _, out var pairwise);

        router.Generate(new GenerateRequest { Strategy = "PAIRWISE" });

        pairwise.Called.Should().BeTrue();
    }

    [Fact]
    public void Generate_UnknownStrategy_ReturnsError()
    {

        var router = BuildRouter(out var cartesian, out var pairwise);

        var result = router.Generate(new GenerateRequest { Strategy = "genetic" });

        cartesian.Called.Should().BeFalse();
        pairwise.Called.Should().BeFalse();
        result.Scenarios.Should().BeEmpty();
        result.Issues.Should().ContainSingle(i =>
            i.Code == "UNKNOWN_STRATEGY" && i.Severity == "error" && i.Message.Contains("genetic"));
    }
}
