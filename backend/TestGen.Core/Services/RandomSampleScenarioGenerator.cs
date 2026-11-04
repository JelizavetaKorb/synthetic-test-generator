using Microsoft.Extensions.Logging;
using TestGen.Core.Generation;

namespace TestGen.Core.Services;

public sealed class RandomSampleScenarioGenerator : IGenerationService
{
    private readonly GeneratorPipeline                      _pipeline;
    private readonly ILogger<RandomSampleScenarioGenerator> _logger;

    public RandomSampleScenarioGenerator(
        GeneratorPipeline                       pipeline,
        ILogger<RandomSampleScenarioGenerator>  logger)
    {
        _pipeline = pipeline;
        _logger   = logger;
    }

    public GenerateResult Generate(GenerateRequest request)
    {
        var inputs = _pipeline.PrepareInputs(request);
        if (inputs.HasErrors || inputs.Profile is null)
            return new GenerateResult { Issues = inputs.Issues };

        var combinations = SampleCombinations(
            CartesianScenarioGenerator.CartesianProduct(inputs.Sweeps),
            Math.Max(1, request.MaxScenarios),
            GeneratorPipeline.SampleSeed);
        var result = _pipeline.MaterializeAndFilter(inputs.Profile, inputs.BaseParameters, combinations);

        _logger.LogInformation(
            "RandomSampleScenarioGenerator: profileId={ProfileId}, scenarios produced={Count}",
            request.ProfileId, result.Scenarios.Count);

        return result;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object>> SampleCombinations(
        IEnumerable<IReadOnlyDictionary<string, object>> source, int size, int seed)
    {
        var random    = new Random(seed);
        var reservoir = new List<(long Index, IReadOnlyDictionary<string, object> Combo)>(size);
        long seen     = 0;

        foreach (var combo in source)
        {
            if (reservoir.Count < size)
                reservoir.Add((seen, combo));
            else
            {
                var slot = random.NextInt64(seen + 1);
                if (slot < size) reservoir[(int)slot] = (seen, combo);
            }
            seen++;
        }

        return reservoir.OrderBy(r => r.Index).Select(r => r.Combo).ToList();
    }
}
