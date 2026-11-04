using Microsoft.Extensions.DependencyInjection;
using TestGen.Core.Models;

namespace TestGen.Core.Services;

public sealed class ScenarioGeneratorRouter : IGenerationService
{
    private readonly IGenerationService _cartesian;
    private readonly IGenerationService _pairwise;
    private readonly IGenerationService _randomSample;

    public ScenarioGeneratorRouter(
        [FromKeyedServices("cartesian")] IGenerationService cartesian,
        [FromKeyedServices("pairwise")]  IGenerationService pairwise,
        [FromKeyedServices("random_sample")] IGenerationService randomSample)
    {
        _cartesian    = cartesian;
        _pairwise     = pairwise;
        _randomSample = randomSample;
    }

    public GenerateResult Generate(GenerateRequest request)
    {
        var strategy = string.IsNullOrWhiteSpace(request.Strategy)
            ? "cartesian"
            : request.Strategy;

        if (strategy.Equals("cartesian", StringComparison.OrdinalIgnoreCase))
            return _cartesian.Generate(request);

        if (strategy.Equals("pairwise", StringComparison.OrdinalIgnoreCase))
            return _pairwise.Generate(request);

        if (strategy.Equals("random_sample", StringComparison.OrdinalIgnoreCase))
            return _randomSample.Generate(request);

        return new GenerateResult
        {
            Issues = new[]
            {
                new ValidationIssue
                {
                    Severity = "error",
                    Code     = "UNKNOWN_STRATEGY",
                    Field    = "strategy",
                    Message  = $"Strategy '{request.Strategy}' is not recognized. " +
                               "Use 'cartesian', 'pairwise' or 'random_sample'."
                }
            }
        };
    }
}
