using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Schema;
using TestGen.Core.Formatting;
using TestGen.Core.Services;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Formatting;

/// <summary>
/// End-to-end checks that the JSON produced by <see cref="ScenarioFormatter"/>
/// is valid against the professor's published JSON Schema
/// (Resources/scenario.schema.json). One test per representative scenario
/// (minimal, fault-injection, ASIL D, sweep) so a regression flags the exact
/// shape that broke.
/// </summary>
public sealed class SchemaComplianceTests
{
    private static readonly ScenarioFormatter Sut = new();
    private static readonly JsonSchema Schema = LoadSchema();

    private static JsonSchema LoadSchema()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "scenario.schema.json");
        return JsonSchema.FromFile(path);
    }

    private static GenerateRequest Req(string strategy = "cartesian") =>
        new() { ProfileId = "test", Strategy = strategy };

    private static void AssertValid(JsonDocument doc)
    {
        var node    = JsonNode.Parse(doc.RootElement.GetRawText());
        var opts    = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var result  = Schema.Evaluate(node, opts);

        // JsonSchema.Net surfaces every failed sub-evaluation, including
        // conditional `if` clauses whose failure is meaningful only as a
        // boolean guard (a failing `if` simply means the `then` is skipped,
        // not that the document is invalid). Filter those out and judge
        // validity from what remains.
        var failures = new List<string>();
        CollectFailures(result, failures);

        var real = failures
            .Where(f => !f.Contains("/if/", StringComparison.Ordinal))
            .ToList();

        if (real.Count == 0) return;

        Assert.Fail(
            $"Scenario does not match schema:\n{string.Join("\n", real)}\n\n--- payload ---\n" +
            JsonSerializer.Serialize(node,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void CollectFailures(EvaluationResults r, List<string> sink)
    {
        if (!r.IsValid && r.Errors is { Count: > 0 })
            foreach (var e in r.Errors)
                sink.Add($"  at {r.InstanceLocation} ({r.EvaluationPath}): {e.Key} -> {e.Value}");

        if (r.Details is null) return;
        foreach (var d in r.Details) CollectFailures(d, sink);
    }

    // -------------------------------------------------------------------
    // 1) Minimal scenario — only required fields + defaults
    // -------------------------------------------------------------------
    [Fact]
    public void Minimal_Scenario_Validates()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .Build();
        s.ScenarioId = "11111111-1111-1111-1111-111111111111";
        s.Name       = "minimal";

        using var doc = Sut.FormatOne(s, null, Req());
        AssertValid(doc);
    }

    // -------------------------------------------------------------------
    // 2) Fault Injection — signals + faults + assertions (with time window)
    // -------------------------------------------------------------------
    [Fact]
    public void FaultInjection_Scenario_Validates()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "SIL")
            .WithParameter("priority",        "high")
            // Signal
            .WithParameter("signalList[0].name",     "engine_rpm")
            .WithParameter("signalList[0].type",     "analog")
            .WithParameter("signalList[0].unit",     "rpm")
            .WithParameter("signalList[0].min",      0)
            .WithParameter("signalList[0].max",      8000)
            .WithParameter("signalList[0].behavior", "ramp")
            // Fault
            .WithParameter("faults[0].faultId",     "f1")
            .WithParameter("faults[0].type",        "sensor_spoof")
            .WithParameter("faults[0].target",      "engine_rpm")
            .WithParameter("faults[0].startTimeMs", 1000)
            .WithParameter("faults[0].durationMs",  2000)
            .WithParameter("faults[0].intensity",   0.8)
            .WithParameter("faults[0].pattern",     "burst")
            .WithParameter("faults[0].repeat",      0)
            // Assertion with nested time_window_ms
            .WithParameter("assertions[0].id",       "a1")
            .WithParameter("assertions[0].type",     "metric")
            .WithParameter("assertions[0].expr",     "engine_rpm < 8000")
            .WithParameter("assertions[0].severity", "critical")
            .WithParameter("assertions[0].timeWindowMs.from", 0)
            .WithParameter("assertions[0].timeWindowMs.to",   10000)
            .Build();
        s.ScenarioId = "22222222-2222-2222-2222-222222222222";
        s.Name       = "fault_injection_test";

        using var doc = Sut.FormatOne(s, null, Req("pairwise"));

        // Sanity: time_window_ms must be a nested object, not a flat dotted key.
        var assertion = doc.RootElement.GetProperty("assertions")[0];
        assertion.TryGetProperty("time_window_ms.from", out _).Should().BeFalse(
            "the formatter must nest time_window_ms.from into an object");
        var tw = assertion.GetProperty("time_window_ms");
        tw.GetProperty("from").GetDouble().Should().Be(0);
        tw.GetProperty("to").GetDouble().Should().Be(10000);

        AssertValid(doc);
    }

    // -------------------------------------------------------------------
    // 3) ASIL D Safety scenario
    // -------------------------------------------------------------------
    [Fact]
    public void AsilD_Scenario_Validates()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",           "SAFETY-ECU-001")
            .WithParameter("targetExecution", "HIL")
            .WithParameter("asilLevel",       "D")
            .WithParameter("hazardId",        "HAZ-001")
            .WithParameter("safetyGoal",      "Prevent unintended acceleration")
            .WithParameter("riskWeight",      0.9)
            .WithParameter("assertions[0].id",       "stl1")
            .WithParameter("assertions[0].type",     "STL")
            .WithParameter("assertions[0].expr",     "G(speed < 50)")
            .WithParameter("assertions[0].severity", "critical")
            .WithParameter("execution.targetRunner", "HIL_bench")
            .Build();
        s.ScenarioId = "33333333-3333-3333-3333-333333333333";
        s.Name       = "asil_d_safety_test";

        using var doc = Sut.FormatOne(s, null, Req());

        // Sanity: execution keys must NOT be double-prefixed.
        var execution = doc.RootElement.GetProperty("execution");
        execution.TryGetProperty("execution.target_runner", out _).Should().BeFalse();
        execution.GetProperty("target_runner").GetString().Should().Be("HIL_bench");

        var safety = doc.RootElement.GetProperty("safety");
        safety.GetProperty("ASIL_level").GetString().Should().Be("D");
        safety.GetProperty("hazard_id").GetString().Should().Be("HAZ-001");

        AssertValid(doc);
    }

    // -------------------------------------------------------------------
    // 4) Environmental perturbations — EMI_level capitalization
    // -------------------------------------------------------------------
    [Fact]
    public void EnvironmentalPerturbations_UseUppercaseEmiKey()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                          "ENGINE-ECU-001")
            .WithParameter("targetExecution",                "simulation")
            .WithParameter("environmentalPerturbations.emiLevel", "high")
            .Build();
        s.ScenarioId = "44444444-4444-4444-4444-444444444444";
        s.Name       = "emi_test";

        using var doc = Sut.FormatOne(s, null, Req());
        var env = doc.RootElement.GetProperty("environmental_perturbations");
        env.TryGetProperty("emi_level", out _).Should().BeFalse();
        env.GetProperty("EMI_level").GetString().Should().Be("high");

        AssertValid(doc);
    }

    // -------------------------------------------------------------------
    // 5) Frontend UI-only keys must NOT leak to root
    // -------------------------------------------------------------------
    [Theory]
    [InlineData("testType",            "unit_test")]
    [InlineData("faultsEnabled",       true)]
    [InlineData("unitTestParams",      "anything")]
    [InlineData("stressEnduranceParams","anything")]
    [InlineData("ecuCatalogEntry",     "anything")]
    public void UiOnlyKeys_AreDroppedFromOutput(string key, object value)
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .WithParameter(key, value)
            .Build();
        s.ScenarioId = "55555555-5555-5555-5555-555555555555";
        s.Name       = "ui_leak_test";

        using var doc = Sut.FormatOne(s, null, Req());
        var snake = ScenarioFormatter.CamelToSnake(key);
        doc.RootElement.TryGetProperty(snake, out _).Should().BeFalse(
            $"'{snake}' is not in the professor's schema and must be dropped");

        AssertValid(doc);
    }

    // -------------------------------------------------------------------
    // 6) Constraint expressions are folded under "advanced"
    // -------------------------------------------------------------------
    [Fact]
    public void ConstraintExpressions_GoUnderAdvanced()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .WithParameter("constraintExpressions", new List<string> { "battery_voltage_V >= 10", "" })
            .Build();
        s.ScenarioId = "66666666-6666-6666-6666-666666666666";
        s.Name       = "constraint_test";

        using var doc = Sut.FormatOne(s, null, Req());

        doc.RootElement.TryGetProperty("constraint_expressions", out _).Should().BeFalse(
            "constraint_expressions must live under advanced, not at the root");

        var advanced = doc.RootElement.GetProperty("advanced");
        var arr = advanced.GetProperty("constraint_expressions");
        arr.GetArrayLength().Should().Be(1, "empty strings must be filtered");
        arr[0].GetString().Should().Be("battery_voltage_V >= 10");

        AssertValid(doc);
    }
}
