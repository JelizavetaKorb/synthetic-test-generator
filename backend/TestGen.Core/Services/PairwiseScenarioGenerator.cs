using Microsoft.Extensions.Logging;
using TestGen.Core.Generation;

namespace TestGen.Core.Services;

public sealed class PairwiseScenarioGenerator : IGenerationService
{
    private readonly GeneratorPipeline                  _pipeline;
    private readonly ILogger<PairwiseScenarioGenerator> _logger;

    public PairwiseScenarioGenerator(
        GeneratorPipeline                   pipeline,
        ILogger<PairwiseScenarioGenerator>  logger)
    {
        _pipeline = pipeline;
        _logger   = logger;
    }

    public GenerateResult Generate(GenerateRequest request)
    {
        var inputs = _pipeline.PrepareInputs(request);
        if (inputs.HasErrors || inputs.Profile is null)
            return new GenerateResult { Issues = inputs.Issues };

        var combinations = GeneratePairwiseCombinations(inputs.Sweeps);
        var result       = _pipeline.MaterializeAndFilter(inputs.Profile, inputs.BaseParameters, combinations);

        _logger.LogInformation(
            "PairwiseScenarioGenerator: profileId={ProfileId}, scenarios produced={Count}",
            request.ProfileId, result.Scenarios.Count);

        return result;
    }

    private static IEnumerable<IReadOnlyDictionary<string, object>> GeneratePairwiseCombinations(
        IReadOnlyList<(string Key, object[] Values)> axes)
    {
        if (axes.Count == 0)
        {
            yield return new Dictionary<string, object>();
            yield break;
        }

        if (axes.Count == 1)
        {
            foreach (var v in axes[0].Values)
                yield return new Dictionary<string, object> { [axes[0].Key] = v };
            yield break;
        }

        var n           = axes.Count;
        var uncovered   = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
                foreach (var a in axes[i].Values)
                    foreach (var b in axes[j].Values)
                        uncovered.Add(PairKey(i, a, j, b));

        var seedIdx = new int[n];
        MarkCovered(axes, seedIdx, uncovered);
        yield return BuildCombo(axes, seedIdx);

        while (uncovered.Count > 0)
        {
            var chosen = new int[n];

            for (var i = 0; i < n; i++)
            {
                var bestIdx   = 0;
                var bestCount = -1;

                for (var vi = 0; vi < axes[i].Values.Length; vi++)
                {
                    var count = 0;

                    for (var j = 0; j < i; j++)
                        if (uncovered.Contains(PairKey(j, axes[j].Values[chosen[j]], i, axes[i].Values[vi])))
                            count++;

                    for (var j = i + 1; j < n; j++)
                        foreach (var vj in axes[j].Values)
                            if (uncovered.Contains(PairKey(i, axes[i].Values[vi], j, vj)))
                                count++;

                    if (count > bestCount)
                    {
                        bestCount = count;
                        bestIdx   = vi;
                    }
                }

                chosen[i] = bestIdx;
            }

            MarkCovered(axes, chosen, uncovered);
            yield return BuildCombo(axes, chosen);
        }
    }

    private static string PairKey(int i, object vi, int j, object vj) =>
        $"{i}:{vi?.ToString() ?? string.Empty}|{j}:{vj?.ToString() ?? string.Empty}";

    private static Dictionary<string, object> BuildCombo(
        IReadOnlyList<(string Key, object[] Values)> axes, int[] indices)
    {
        var combo = new Dictionary<string, object>(axes.Count);
        for (var k = 0; k < axes.Count; k++)
            combo[axes[k].Key] = axes[k].Values[indices[k]];
        return combo;
    }

    private static void MarkCovered(
        IReadOnlyList<(string Key, object[] Values)> axes,
        int[]                                         indices,
        HashSet<string>                               uncovered)
    {
        var n = axes.Count;
        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
                uncovered.Remove(PairKey(i, axes[i].Values[indices[i]], j, axes[j].Values[indices[j]]));
    }
}
