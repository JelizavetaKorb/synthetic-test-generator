using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Domain;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Domain;

public sealed class BrakeMissingWheelSpeedRuleTests
{
    private static readonly EcuCatalogEntry BrakeEcu = new()
    {
        EcuId    = "BRAKE-ECU-001",
        EcuName  = "Brake ECU",
        EcuClass = "Brake"
    };

    private static readonly EcuCatalogEntry EngineEcu = new()
    {
        EcuId    = "ENGINE-ECU-001",
        EcuName  = "Engine ECU",
        EcuClass = "Engine"
    };

    private static BrakeMissingWheelSpeedRule Build(params EcuCatalogEntry[] entries) =>
        new(new FakeEcuCatalogService(entries));

    private static readonly Profile AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void NotBrakeEcu_NoIssue()
    {
        var rule = Build(EngineEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                   "ENGINE-ECU-001")
            .WithParameter("assertions[0].expr",      "wheel_speed > 0")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void BrakeEcu_NoWheelSpeedAssertion_NoIssue()
    {
        var rule = Build(BrakeEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",              "BRAKE-ECU-001")
            .WithParameter("assertions[0].expr", "brake_pressure > 10")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void BrakeEcu_WheelSpeedAssertion_NoMessage_Error()
    {
        var rule = Build(BrakeEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",              "BRAKE-ECU-001")
            .WithParameter("assertions[0].expr", "wheel_speed_fl > 0")
            .Build();

        var issues = rule.Check(s, AnyProfile).ToList();
        issues.Should().ContainSingle(i =>
            i.Code == BrakeMissingWheelSpeedRule.RuleCode && i.Severity == "error");
    }

    [Fact]
    public void BrakeEcu_WheelSpeedAssertion_MessageDeclared_NoIssue()
    {
        var rule = Build(BrakeEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                  "BRAKE-ECU-001")
            .WithParameter("assertions[0].expr",     "wheel_speed_fl > 0")
            .WithParameter("messageDefs[0].id",      "MSG_WHEEL_SPEED")
            .WithParameter("messageDefs[0].name",    "wheel_speed_msg")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void BrakeEcu_WheelSpeedAssertion_SignalDeclared_NoIssue()
    {
        var rule = Build(BrakeEcu);
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",                  "BRAKE-ECU-001")
            .WithParameter("assertions[0].expr",     "wheel_speed > 0")
            .WithParameter("signalList[0].name",     "wheel_speed_fl")
            .Build();

        rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void BrakeEcu_WheelSpeedAssertionFromProfileArray_NoSource_Error()
    {
        var s = new ScenarioBuilder()
            .WithParameter("ecuId",      "BRAKE-ECU-001")
            .WithParameter("assertions", JsonDocument.Parse("""[{"expr":"wheel_speed_fl > 0"}]""").RootElement)
            .Build();

        Build(BrakeEcu).Check(s, AnyProfile).Should().ContainSingle(i =>
            i.Code == BrakeMissingWheelSpeedRule.RuleCode);
    }
}
