using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Safety;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Safety;

public sealed class AsilRequiresHazardRuleTests
{
    private static readonly AsilRequiresHazardRule Rule       = new();
    private static readonly Profile                AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_NoAsil_NoIssue()
    {
        Rule.Check(new ScenarioBuilder().Build(), AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_AsilQm_NoIssue()
    {
        var s = new ScenarioBuilder().WithParameter("asilLevel", "QM").Build();
        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_AsilB_NoHazard_Error()
    {
        var s = new ScenarioBuilder().WithParameter("asilLevel", "B").Build();
        var issues = Rule.Check(s, AnyProfile).ToList();
        issues.Should().Contain(i => i.Field == "hazardId" && i.Severity == "error");
    }

    [Fact]
    public void Check_AsilC_HazardButNoCritical_Error()
    {
        var s = new ScenarioBuilder()
            .WithParameter("asilLevel",                "C")
            .WithParameter("hazardId",                 "HAZ-001")
            .WithParameter("assertions[0].severity",   "major")
            .Build();

        var issues = Rule.Check(s, AnyProfile).ToList();
        issues.Should().Contain(i => i.Field == "asilLevel" && i.Severity == "error");
    }

    [Fact]
    public void Check_AsilC_WithHazardAndCritical_NoIssue()
    {
        var s = new ScenarioBuilder()
            .WithParameter("asilLevel",                "C")
            .WithParameter("hazardId",                 "HAZ-001")
            .WithParameter("assertions[0].severity",   "critical")
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }
}
