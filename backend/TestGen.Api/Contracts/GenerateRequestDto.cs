using System.ComponentModel.DataAnnotations;
using TestGen.Core.Services;

namespace TestGen.Api.Contracts;

public sealed class SweepSpecDto
{
    public string Type { get; set; } = "fixed";
    public double? From { get; set; }
    public double? To { get; set; }
    public double? Step { get; set; }
    public object[]? Values { get; set; }
    public int? Count { get; set; }

    public SweepSpec ToDomain() => new()
    {
        Type = Type,
        From = From,
        To = To,
        Step = Step,
        Values = Values,
        Count = Count
    };
}

public sealed class GenerateRequestDto
{
    [Required]
    public string ProfileId { get; set; } = string.Empty;

    public Dictionary<string, object>? Overrides { get; set; }

    public Dictionary<string, SweepSpecDto>? SweepParameters { get; set; }

    public string Strategy { get; set; } = "cartesian";

    public int MaxScenarios { get; set; } = 100;

    public GenerateRequest ToDomain() => new()
    {
        ProfileId = ProfileId,
        Overrides = Overrides,
        SweepParameters = SweepParameters?.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToDomain()),
        Strategy = Strategy,
        MaxScenarios = MaxScenarios
    };
}
