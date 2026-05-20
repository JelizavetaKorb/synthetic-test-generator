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
        // Arrange
        var router = BuildRouter(out var cartesian, out var pairwise);

        // Act
        var result = router.Generate(new GenerateRequest { Strategy = "cartesian" });

        // Assert
        cartesian.Called.Should().BeTrue();
        pairwise.Called.Should().BeFalse();
        result.Scenarios[0].Parameters["label"].Should().Be("cartesian");
    }

    [Fact]
    public void Generate_PairwiseStrategy_DelegatesToPairwise()
    {
        // Arrange
        var router = BuildRouter(out var cartesian, out var pairwise);

        // Act
        var result = router.Generate(new GenerateRequest { Strategy = "pairwise" });

        // Assert
        pairwise.Called.Should().BeTrue();
        cartesian.Called.Should().BeFalse();
        result.Scenarios[0].Parameters["label"].Should().Be("pairwise");
    }

    [Fact]
    public void Generate_EmptyStrategy_DefaultsToCartesian()
    {
        // Arrange
        var router = BuildRouter(out var cartesian, out _);

        // Act
        router.Generate(new GenerateRequest { Strategy = string.Empty });

        // Assert
        cartesian.Called.Should().BeTrue();
    }

    [Fact]
    public void Generate_StrategyIsCaseInsensitive_DelegatesToCorrectGenerator()
    {
        // Arrange
        var router = BuildRouter(out _, out var pairwise);

        // Act
        router.Generate(new GenerateRequest { Strategy = "PAIRWISE" });

        // Assert
        pairwise.Called.Should().BeTrue();
    }

    [Fact]
    public void Generate_UnknownStrategy_ReturnsError()
    {
        // Arrange
        var router = BuildRouter(out var cartesian, out var pairwise);

        // Act
        var result = router.Generate(new GenerateRequest { Strategy = "genetic" });

        // Assert
        cartesian.Called.Should().BeFalse();
        pairwise.Called.Should().BeFalse();
        result.Scenarios.Should().BeEmpty();
        result.Issues.Should().ContainSingle(i =>
            i.Code == "UNKNOWN_STRATEGY" && i.Severity == "error" && i.Message.Contains("genetic"));
    }
}
