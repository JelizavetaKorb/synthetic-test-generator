using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Domain;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Domain;

public sealed class BmsOvervoltageRuleTests
{
    private static readonly EcuCatalogEntry BmsEcu = new()
    {
        EcuId                 = "BMS-001",
        EcuName               = "BMS",
        EcuClass              = "BMS",
        MinOperationalVoltage = 8.0,
        MaxOperationalVoltage = 16.5
    };

    private static readonly EcuCatalogEntry EngineEcu = new()
    {
        EcuId                 = "ENGINE-ECU-001",
        EcuClass              = "Engine",
        MaxOperationalVoltage = 16.0
    };

    private static BmsOvervoltageRule Build(params EcuCatalogEntry[] entries) =>
        new(new FakeEcuCatalogService(entries));

    private static readonly Profile AnyProfile  = new ProfileBuilder().Build();
    private static readonly Profile SafetyAsilD = new ProfileBuilder().WithId("safety-critical-asil-d").Build();

    [Fact]
    public void NotBms_NoIssue()
    {
        var rule = Build(EngineEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "ENGINE-ECU-001")
            .WithParameter("initialConditions.batteryVoltageV",    18.0)
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Bms_VoltageWithinRange_NoIssue()
    {
        var rule = Build(BmsEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "BMS-001")
            .WithParameter("initialConditions.batteryVoltageV",    14.0)
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Bms_VoltageAboveMax_NoSafetyTest_Error()
    {
        var rule = Build(BmsEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "BMS-001")
            .WithParameter("initialConditions.batteryVoltageV",    20.0)
            .Build();

        rule.Check(s, AnyProfile).Should().ContainSingle(i =>
            i.Code == BmsOvervoltageRule.RuleCode && i.Severity == "error");
    }

    [Fact]
    public void Bms_VoltageAboveMax_SafetyOvervoltageTestType_NoIssue()
    {
        var rule = Build(BmsEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "BMS-001")
            .WithParameter("initialConditions.batteryVoltageV",    20.0)
            .WithParameter("testType",                             "safety_overvoltage_test")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Bms_VoltageAboveMax_SafetyCriticalProfile_NoIssue()
    {
        var rule = Build(BmsEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                                "BMS-001")
            .WithParameter("initialConditions.batteryVoltageV",    20.0)
            .Build();

        rule.Check(s, SafetyAsilD).Should().BeEmpty();
    }
}
