using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Domain;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Domain;

public sealed class EngineColdStartFuelRuleTests
{
    private static readonly EcuCatalogEntry EngineEcu = new()
    {
        EcuId    = "ENGINE-ECU-001",
        EcuName  = "Engine ECU",
        EcuClass = "Engine"
    };

    private static readonly EcuCatalogEntry BrakeEcu = new()
    {
        EcuId    = "BRAKE-ECU-001",
        EcuName  = "Brake ECU",
        EcuClass = "Brake"
    };

    private static EngineColdStartFuelRule Build(params EcuCatalogEntry[] entries) =>
        new(new FakeEcuCatalogService(entries));

    private static readonly Profile AnyProfile      = new ProfileBuilder().Build();
    private static readonly Profile ColdStartProfile = new ProfileBuilder().WithId("cold-start").Build();

    [Fact]
    public void NotEngineEcu_NoIssue()
    {
        var rule = Build(BrakeEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "BRAKE-ECU-001")
            .WithParameter("initialConditions.fuelLevelPercent",   0.0)
            .WithParameter("testType",                             "cold_start")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void EngineEcu_FuelAboveZero_NoIssue()
    {
        var rule = Build(EngineEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "ENGINE-ECU-001")
            .WithParameter("initialConditions.fuelLevelPercent",   5.0)
            .WithParameter("testType",                             "cold_start")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void EngineEcu_FuelZero_NotColdStart_NoIssue()
    {
        var rule = Build(EngineEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "ENGINE-ECU-001")
            .WithParameter("initialConditions.fuelLevelPercent",   0.0)
            .WithParameter("testType",                             "regression")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void EngineEcu_FuelZero_ColdStartTestType_Error()
    {
        var rule = Build(EngineEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "ENGINE-ECU-001")
            .WithParameter("initialConditions.fuelLevelPercent",   0.0)
            .WithParameter("testType",                             "cold_start")
            .Build();

        rule.Check(s, AnyProfile).Should().ContainSingle(i =>
            i.Code == EngineColdStartFuelRule.RuleCode && i.Severity == "error");
    }

    [Fact]
    public void EngineEcu_FuelZero_ColdStartProfile_Error()
    {
        var rule = Build(EngineEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "ENGINE-ECU-001")
            .WithParameter("initialConditions.fuelLevelPercent",   0.0)
            .Build();

        rule.Check(s, ColdStartProfile).Should().ContainSingle(i =>
            i.Code == EngineColdStartFuelRule.RuleCode);
    }
}
