using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Schema;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestGen.Tests.Integration;

public sealed class ProfileOutputTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSchema Schema = JsonSchema.FromFile(
        Path.Combine(AppContext.BaseDirectory, "Resources", "scenario.schema.json"));

    private readonly HttpClient _client;

    public ProfileOutputTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    public static IEnumerable<object[]> ProfileIds() => new[]
    {
        "cold-start", "hot-start", "regulation-certification",
        "fault-injection-bus", "stress-endurance",
        "safety-critical-asil-a", "safety-critical-asil-b",
        "safety-critical-asil-c", "safety-critical-asil-d",
        "functional-regression", "cyberattack-spoofing"
    }.Select(id => new object[] { id });

    private async Task<JsonElement> Generate(object body)
    {
        var resp = await _client.PostAsJsonAsync("/api/scenarios/generate", body);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Theory]
    [MemberData(nameof(ProfileIds))]
    public async Task EveryProfile_GeneratesSchemaValidScenario(string profileId)
    {
        var result = await Generate(new { profileId, strategy = "cartesian", maxScenarios = 1 });

        result.GetProperty("generatedCount").GetInt32().Should().Be(1,
            $"profile '{profileId}' should produce a scenario; issues: {result.GetProperty("issues")}");

        var scenario = JsonNode.Parse(result.GetProperty("scenarios")[0].GetRawText());
        var eval = Schema.Evaluate(scenario, new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true });
        var failures = (eval.Details ?? Array.Empty<EvaluationResults>())
            .Where(d => !d.IsValid && d.Errors is { Count: > 0 })
            .Select(d => $"{d.InstanceLocation}: {string.Join("; ", d.Errors!.Values)}");

        eval.IsValid.Should().BeTrue(
            $"profile '{profileId}' output must match the scenario schema: {string.Join(" | ", failures)}");
    }

    [Fact]
    public async Task EveryProfileEcu_ExistsInCatalog()
    {
        var ecus = await _client.GetFromJsonAsync<JsonElement>("/api/ecus");
        var ecuIds = ecus.EnumerateArray().Select(e => e.GetProperty("ecuId").GetString()).ToHashSet();

        var profiles = await _client.GetFromJsonAsync<JsonElement>("/api/profiles");
        foreach (var profile in profiles.EnumerateArray())
        {
            if (!profile.GetProperty("defaultParameters").TryGetProperty("ecuId", out var ecuId)) continue;
            ecuIds.Should().Contain(ecuId.GetString(),
                $"profile '{profile.GetProperty("profileId").GetString()}' must use an ECU from the catalog");
        }
    }

    [Fact]
    public async Task ColdStart_WithFlexRayOverride_RejectsInvalidBitrate()
    {
        var result = await Generate(new
        {
            profileId = "cold-start",
            strategy  = "cartesian",
            maxScenarios = 1,
            overrides = new Dictionary<string, object>
            {
                ["bus.busType"]     = "FlexRay",
                ["bus.bitrateKbps"] = 1000
            }
        });

        result.GetProperty("generatedCount").GetInt32().Should().Be(0);
        result.GetProperty("issues").EnumerateArray()
            .Select(i => i.GetProperty("code").GetString())
            .Should().Contain("BUS_BITRATE_FLEXRAY");
    }

    [Fact]
    public async Task UserOverrides_WinOverProfileDefaults_InOutput()
    {
        var result = await Generate(new
        {
            profileId = "safety-critical-asil-a",
            strategy  = "cartesian",
            maxScenarios = 1,
            overrides = new Dictionary<string, object>
            {
                ["riskWeight"]       = 0.9,
                ["bus.busLoadPct"]   = 33
            }
        });

        var scenario = result.GetProperty("scenarios")[0];
        scenario.GetProperty("safety").GetProperty("risk_weight").GetDouble().Should().Be(0.9);
        scenario.GetProperty("bus_config").GetProperty("bus_load_pct").GetDouble().Should().Be(33);
    }
}
