using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestGen.Tests.Integration;

/// <summary>
/// End-to-end tests that boot the real ASP.NET Core pipeline in-memory and
/// hit it through HttpClient. They exercise DI wiring, JSON contracts and the
/// full profile → generator → formatter pipeline as a black box.
/// </summary>
public sealed class ApiEndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiEndToEndTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProfiles_ReturnsAllElevenSpecProfiles()
    {
        var resp = await _client.GetAsync("/api/profiles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var ids = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("profileId").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        ids.Should().Contain(new[]
        {
            "cold-start", "hot-start", "regulation-certification",
            "fault-injection-bus", "stress-endurance",
            "safety-critical-asil-a", "safety-critical-asil-b",
            "safety-critical-asil-c", "safety-critical-asil-d",
            "functional-regression", "cyberattack-spoofing"
        });
    }

    [Fact]
    public async Task GetEcus_ReturnsCatalogWithEcuClass()
    {
        var resp = await _client.GetAsync("/api/ecus");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var classes = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("ecuClass").GetString()!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        classes.Should().Contain(new[] { "Engine", "Brake", "BMS", "ADAS" });
    }

    [Fact]
    public async Task GenerateColdStart_ProducesValidScenario()
    {
        var resp = await _client.PostAsJsonAsync("/api/scenarios/generate", new
        {
            profileId    = "cold-start",
            strategy     = "cartesian",
            maxScenarios = 1
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("generatedCount").GetInt32().Should().Be(1);

        var scenario = doc.RootElement.GetProperty("scenarios")[0];
        scenario.GetProperty("scenario_id").GetString().Should().NotBeNullOrWhiteSpace();
        scenario.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
        scenario.GetProperty("target_execution").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GenerateWithUnknownProfile_ReturnsValidationIssue()
    {
        var resp = await _client.PostAsJsonAsync("/api/scenarios/generate", new
        {
            profileId    = "does-not-exist",
            strategy     = "cartesian",
            maxScenarios = 1
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("generatedCount").GetInt32().Should().Be(0);
        doc.RootElement.GetProperty("issues").EnumerateArray()
            .Should().Contain(i => i.GetProperty("code").GetString() == "PROFILE_NOT_FOUND");
    }

    [Fact]
    public async Task GenerateWithCartesianExplosion_IsRejected()
    {
        // 11 * 11 * 11 = 1331 > default MaxScenarios (100): must trip
        // CARTESIAN_EXPLOSION before materialising any scenario.
        var resp = await _client.PostAsJsonAsync("/api/scenarios/generate", new
        {
            profileId    = "cold-start",
            strategy     = "cartesian",
            maxScenarios = 100,
            sweepParameters = new
            {
                a = new { type = "linear_range", from = 0.0, to = 10.0, step = 1.0 },
                b = new { type = "linear_range", from = 0.0, to = 10.0, step = 1.0 },
                c = new { type = "linear_range", from = 0.0, to = 10.0, step = 1.0 }
            }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("generatedCount").GetInt32().Should().Be(0);
        doc.RootElement.GetProperty("issues").EnumerateArray()
            .Should().Contain(i => i.GetProperty("code").GetString() == "CARTESIAN_EXPLOSION");
    }

    [Fact]
    public async Task BmsOvervoltage_IsRejected_EndToEnd()
    {
        // Real EcuCatalog.json declares BMS-001 with maxOperationalVoltage=16.5
        // Sending 20V without a safety_overvoltage_test must surface the new
        // rule 19 (BMS_OVERVOLTAGE_WITHOUT_SAFETY_TEST) as a validation issue.
        var resp = await _client.PostAsJsonAsync("/api/scenarios/generate", new
        {
            strategy     = "cartesian",
            maxScenarios = 1,
            overrides = new Dictionary<string, object>
            {
                ["name"]                                = "bms ov test",
                ["ecuId"]                               = "BMS-001",
                ["targetExecution"]                     = "simulation",
                ["initialConditions.batteryVoltageV"]   = 20.0
            }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        doc.RootElement.GetProperty("issues").EnumerateArray()
            .Should().Contain(i =>
                i.GetProperty("code").GetString() == "BMS_OVERVOLTAGE_WITHOUT_SAFETY_TEST");
    }
}
