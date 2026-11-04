using TestGen.Core.Models;

namespace TestGen.Core.Services;

public sealed class GenerateRequest
{
    public const int    DefaultMaxScenarios = 100;
    public const string DefaultStrategy     = "cartesian";

    public string ProfileId { get; set; } = string.Empty;
    public Dictionary<string, object>? Overrides { get; set; }
    public Dictionary<string, SweepSpec>? SweepParameters { get; set; }
    public string Strategy { get; set; } = DefaultStrategy;
    public int MaxScenarios { get; set; } = DefaultMaxScenarios;
}

public sealed class SweepSpec
{
    public string Type { get; set; } = "fixed";
    public double? From { get; set; }
    public double? To { get; set; }
    public double? Step { get; set; }
    public object[]? Values { get; set; }
    public int? Count { get; set; }
    public double? Mean { get; set; }
    public double? Std { get; set; }
}

public sealed class GenerateResult
{
    public IReadOnlyList<Scenario> Scenarios { get; set; } = Array.Empty<Scenario>();
    public IReadOnlyList<ValidationIssue> Issues { get; set; } = Array.Empty<ValidationIssue>();
}

public interface IGenerationService
{
    GenerateResult Generate(GenerateRequest request);
}
