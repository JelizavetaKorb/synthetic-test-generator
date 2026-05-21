using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Core.Validation.Domain;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Domain;

public sealed class BatteryBelowEcuMinimumRuleTests
{
    private static BatteryBelowEcuMinimumRule BuildRule(params EcuCatalogEntry[] entries) =>
        new(new FakeEcuCatalogService(entries));

    private static readonly EcuCatalogEntry EngineEcu = new()
    {
        EcuId                 = "ENGINE-ECU-001",
        EcuName               = "Primary Engine ECU",
        MinOperationalVoltage = 9.0,
        MaxOperationalVoltage = 16.0
    };

    [Fact]
    public void Check_VoltageBelowMinimum_ReturnsError()
    {

        var rule = BuildRule(EngineEcu);
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",                                  "ENGINE-ECU-001")
            .WithParameter("initialConditions.batteryVoltageV",      7.0)
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var issues = rule.Check(scenario, profile).ToList();

        issues.Should().ContainSingle();
        issues[0].Severity.Should().Be("error");
        issues[0].Code.Should().Be(BatteryBelowEcuMinimumRule.RuleCode);
        issues[0].Field.Should().Be("initialConditions.batteryVoltageV");
        issues[0].Message.Should().Contain("7");
        issues[0].Message.Should().Contain("9");
    }

    [Fact]
    public void Check_VoltageAboveMinimum_ReturnsNoIssues()
    {

        var rule = BuildRule(EngineEcu);
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",                             "ENGINE-ECU-001")
            .WithParameter("initialConditions.batteryVoltageV", 12.0)
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var issues = rule.Check(scenario, profile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_VoltageExactlyAtMinimum_ReturnsNoIssues()
    {

        var rule = BuildRule(EngineEcu);
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",                             "ENGINE-ECU-001")
            .WithParameter("initialConditions.batteryVoltageV", 9.0)
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var issues = rule.Check(scenario, profile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_EcuNotInCatalog_ReturnsNoIssues()
    {

        var rule = BuildRule(EngineEcu);
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId",                             "UNKNOWN-ECU")
            .WithParameter("initialConditions.batteryVoltageV", 7.0)
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var issues = rule.Check(scenario, profile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_MissingEcuId_ReturnsNoIssues()
    {

        var rule = BuildRule(EngineEcu);
        var scenario = new ScenarioBuilder()
            .WithParameter("initialConditions.batteryVoltageV", 7.0)
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var issues = rule.Check(scenario, profile);

        issues.Should().BeEmpty();
    }

    [Fact]
    public void Check_MissingVoltage_ReturnsNoIssues()
    {

        var rule = BuildRule(EngineEcu);
        var scenario = new ScenarioBuilder()
            .WithParameter("ecuId", "ENGINE-ECU-001")
            .Build();
        var profile = new ProfileBuilder().WithId("p").Build();

        var issues = rule.Check(scenario, profile);

        issues.Should().BeEmpty();
    }
}

internal sealed class FakeEcuCatalogService : IEcuCatalogService
{
    private readonly EcuCatalogEntry[] _entries;

    public FakeEcuCatalogService(params EcuCatalogEntry[] entries) => _entries = entries;

    public IReadOnlyList<EcuCatalogEntry> GetAll() => _entries;

    public EcuCatalogEntry? GetById(string ecuId) =>
        _entries.FirstOrDefault(e => e.EcuId.Equals(ecuId, StringComparison.OrdinalIgnoreCase));
}
