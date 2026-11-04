using Microsoft.Extensions.Logging;
using TestGen.Core.Generation;

namespace TestGen.Core.Services;

public sealed class CartesianScenarioGenerator : IGenerationService
{
    private readonly GeneratorPipeline                 _pipeline;
    private readonly ILogger<CartesianScenarioGenerator> _logger;

    public CartesianScenarioGenerator(
        GeneratorPipeline                  pipeline,
        ILogger<CartesianScenarioGenerator> logger)
    {
        _pipeline = pipeline;
        _logger   = logger;
    }

    public GenerateResult Generate(GenerateRequest request)
    {
        var inputs = _pipeline.PrepareInputs(request);
        if (inputs.HasErrors || inputs.Profile is null)
            return new GenerateResult { Issues = inputs.Issues };

        if (inputs.Sweeps.Count > 0)
        {
            long productSize = 1;
            foreach (var (_, vals) in inputs.Sweeps)
                productSize *= vals.Length;

            if (productSize > request.MaxScenarios)
                return new GenerateResult
                {
                    Issues = new[]
                    {
                        new Models.ValidationIssue
                        {
                            Severity = "error",
                            Code     = "CARTESIAN_EXPLOSION",
                            Field    = "sweepParameters",
                            Message  = $"Cartesian product would generate {productSize} scenarios, exceeding the limit of {request.MaxScenarios}. Consider using 'pairwise' strategy to reduce the count."
                        }
                    }
                };
        }

        var combinations = CartesianProduct(inputs.Sweeps);
        var result       = _pipeline.MaterializeAndFilter(inputs.Profile, inputs.BaseParameters, combinations);

        _logger.LogInformation(
            "CartesianScenarioGenerator: profileId={ProfileId}, scenarios produced={Count}",
            request.ProfileId, result.Scenarios.Count);

        return result;
    }

    internal static IEnumerable<IReadOnlyDictionary<string, object>> CartesianProduct(
        IReadOnlyList<(string Key, object[] Values)> axes)
    {
        if (axes.Count == 0)
        {
            yield return new Dictionary<string, object>();
            yield break;
        }

        var indices = new int[axes.Count];

        while (true)
        {
            var combo = new Dictionary<string, object>(axes.Count);
            for (var i = 0; i < axes.Count; i++)
                combo[axes[i].Key] = axes[i].Values[indices[i]];

            yield return combo;

            var pos = axes.Count - 1;
            while (pos >= 0)
            {
                indices[pos]++;
                if (indices[pos] < axes[pos].Values.Length) break;
                indices[pos] = 0;
                pos--;
            }

            if (pos < 0) yield break;
        }
    }
}
