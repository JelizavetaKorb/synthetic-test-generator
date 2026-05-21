using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Formatting;
using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Formatting;

public sealed class ScenarioFormatterTests
{
    private static readonly ScenarioFormatter Sut = new();

    private static GenerateRequest DefaultRequest(string strategy = "cartesian") => new()
    {
        ProfileId = "test",
        Strategy  = strategy
    };

    [Fact]
    public void Format_MinimalScenario_HasRequiredTopLevelFields()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .Build();
        scenario.ScenarioId = "test-id-001";
        scenario.Name       = "Cold Start #001";
        var profile = new ProfileBuilder().WithId("cold-start").Build();

        using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
        var root = doc.RootElement;

        root.GetProperty("scenario_id").GetString().Should().Be("test-id-001");
        root.GetProperty("name").GetString().Should().Be("Cold Start #001");
        root.GetProperty("ecu_id").GetString().Should().Be("ENGINE-ECU-001");
        root.GetProperty("target_execution").GetString().Should().Be("simulation");
        root.TryGetProperty("created_at", out var ca).Should().BeTrue();
        ca.GetString().Should().NotBeNullOrEmpty();

        root.TryGetProperty("ecuId", out _).Should().BeFalse();
        root.TryGetProperty("targetExecution", out _).Should().BeFalse();
    }

    [Fact]
    public void Format_InitialConditions_NestedCorrectly()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",                                    "ENGINE-ECU-001")
            .WithParameter("targetExecution",                          "HIL")
            .WithParameter("initialConditions.ambientTemperatureC",    -20.0)
            .WithParameter("initialConditions.batteryVoltageV",        11.8)
            .WithParameter("initialConditions.engineState",            "OFF")
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
        var root = doc.RootElement;

        root.TryGetProperty("initial_conditions", out var ic).Should().BeTrue();
        ic.GetProperty("ambient_temperature_C").GetDouble().Should().Be(-20.0);
        ic.GetProperty("battery_voltage_V").GetDouble().Should().Be(11.8);
        ic.GetProperty("engine_state").GetString().Should().Be("OFF");

        root.TryGetProperty("initialConditions.ambientTemperatureC", out _).Should().BeFalse();
        root.TryGetProperty("initialConditions.batteryVoltageV",     out _).Should().BeFalse();
    }

    [Fact]
    public void Format_Faults_ArrayPreserved()
    {

        var fault = new Dictionary<string, object>
        {
            ["faultId"]     = "f1",
            ["type"]        = "message_loss",
            ["target"]      = "MSG_ENGINE_STATUS",
            ["startTimeMs"] = 1000.0,
            ["durationMs"]  = 3000.0
        };
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "HIL")
            .WithParameter("faults",          new List<object> { fault })
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
        var root = doc.RootElement;

        root.TryGetProperty("faults", out var faults).Should().BeTrue();
        faults.ValueKind.Should().Be(JsonValueKind.Array);
        var f0 = faults[0];
        f0.GetProperty("fault_id").GetString().Should().Be("f1");
        f0.GetProperty("type").GetString().Should().Be("message_loss");
        f0.GetProperty("target").GetString().Should().Be("MSG_ENGINE_STATUS");
        f0.GetProperty("start_time_ms").GetDouble().Should().Be(1000.0);
        f0.GetProperty("duration_ms").GetDouble().Should().Be(3000.0);

        f0.TryGetProperty("faultId",     out _).Should().BeFalse();
        f0.TryGetProperty("startTimeMs", out _).Should().BeFalse();
        f0.TryGetProperty("durationMs",  out _).Should().BeFalse();
    }

    [Fact]
    public void Format_SafetyFields_GroupedIntoSafetySection()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "HIL")
            .WithParameter("asilLevel",       "D")
            .WithParameter("hazardId",        "HAZ-001")
            .WithParameter("safetyGoal",      "Prevent unintended acceleration")
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
        var root = doc.RootElement;

        root.TryGetProperty("safety", out var safety).Should().BeTrue();
        safety.GetProperty("ASIL_level").GetString().Should().Be("D");
        safety.GetProperty("hazard_id").GetString().Should().Be("HAZ-001");
        safety.GetProperty("safety_goal").GetString().Should().Be("Prevent unintended acceleration");

        root.TryGetProperty("asilLevel", out _).Should().BeFalse();
        root.TryGetProperty("hazardId",  out _).Should().BeFalse();
    }

    [Fact]
    public void Format_EmptySections_Omitted()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
        var root = doc.RootElement;

        root.TryGetProperty("timing",     out _).Should().BeFalse();
        root.TryGetProperty("faults",     out _).Should().BeFalse();
        root.TryGetProperty("assertions", out _).Should().BeFalse();
    }

    [Fact]
    public void Format_ProfileIncluded()
    {

        var profile = new ProfileBuilder()
            .WithId("cold-start")
            .WithDefault("ecuId", "ENGINE-ECU-001")
            .WithLocked("initialConditions.engineState")
            .WithAssertion("startupTimeMs < 3000")
            .Build();
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",           "ENGINE-ECU-001")
            .WithParameter("targetExecution", "simulation")
            .Build();

        using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
        var root = doc.RootElement;

        root.TryGetProperty("profile", out var profileNode).Should().BeTrue();
        profileNode.GetProperty("profile_id").GetString().Should().Be("cold-start");
        profileNode.GetProperty("name").GetString().Should().Be("cold-start");
        profileNode.TryGetProperty("recommended_assertions", out _).Should().BeTrue();

        profileNode.TryGetProperty("locked_parameters", out _).Should().BeFalse();
        profileNode.TryGetProperty("default_parameters", out _).Should().BeFalse();
    }

    [Fact]
    public void Format_UnknownKeys_PassedThroughAsSnakeCase()
    {

        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",              "ENGINE-ECU-001")
            .WithParameter("targetExecution",    "simulation")
            .WithParameter("customField.someValue", 42)
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var act = () =>
        {
            using var doc = Sut.FormatOne(scenario, profile, DefaultRequest());
            return doc.RootElement.Clone();
        };
        var root = act.Should().NotThrow().Subject;

        var json = root.GetRawText();
        json.Should().Contain("some_value");
        json.Should().NotContain("someValue");
        json.Should().NotContain("customField");
    }

    [Fact]
    public void CamelToSnake_KnownConversions_AreCorrect()
    {
        ScenarioFormatter.CamelToSnake("ambientTemperatureC").Should().Be("ambient_temperature_c");
        ScenarioFormatter.CamelToSnake("batteryVoltageV").Should().Be("battery_voltage_v");
        ScenarioFormatter.CamelToSnake("vehicleSpeedMps").Should().Be("vehicle_speed_mps");
        ScenarioFormatter.CamelToSnake("testDurationMs").Should().Be("test_duration_ms");
        ScenarioFormatter.CamelToSnake("faultId").Should().Be("fault_id");
        ScenarioFormatter.CamelToSnake("startTimeMs").Should().Be("start_time_ms");
    }
}
