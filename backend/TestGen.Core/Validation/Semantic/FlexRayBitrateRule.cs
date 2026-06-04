using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

/// <summary>
/// Rule 15 / §6.2 #7: FlexRay only operates at the discrete rates {2500, 5000,
/// 10000} kbps. Any other bitrate is a configuration error.
/// </summary>
public sealed class FlexRayBitrateRule : IValidationRule
{
    public const string RuleCode = "BUS_BITRATE_FLEXRAY";

    private static readonly double[] AllowedKbps = { 2500.0, 5000.0, 10000.0 };

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "bus.busType", out var busType) &&
            !ParameterAccessor.TryGetString(p, "busType",     out busType))
            yield break;
        if (!busType.Equals("FlexRay", StringComparison.OrdinalIgnoreCase)) yield break;

        if (!ParameterAccessor.TryGetDouble(p, "bus.bitrateKbps", out var rate) &&
            !ParameterAccessor.TryGetDouble(p, "bitrateKbps",     out rate))
            yield break;

        if (Array.IndexOf(AllowedKbps, rate) < 0)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "bus.bitrateKbps",
                Message  = $"FlexRay only supports 2500, 5000 or 10000 kbps (got {rate})."
            };
    }
}
