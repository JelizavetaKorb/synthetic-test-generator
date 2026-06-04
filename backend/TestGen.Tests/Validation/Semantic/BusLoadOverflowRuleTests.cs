using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class BusLoadOverflowRuleTests
{
    private static readonly BusLoadOverflowRule Rule       = new();
    private static readonly Profile             AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_TotalUnder80_NoIssue()
    {
        var s = new ScenarioBuilder()
            .WithParameter("bus.busLoadPct",        50.0)
            .WithParameter("bus.backgroundLoadPct", 10.0)
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_TotalBetween80And100_Warning()
    {
        var s = new ScenarioBuilder()
            .WithParameter("bus.busLoadPct",        70.0)
            .WithParameter("bus.backgroundLoadPct", 15.0)
            .Build();

        Rule.Check(s, AnyProfile).Should().ContainSingle()
            .Which.Severity.Should().Be("warning");
    }

    [Fact]
    public void Check_TotalOver100_Error()
    {
        var s = new ScenarioBuilder()
            .WithParameter("bus.busLoadPct",        80.0)
            .WithParameter("bus.backgroundLoadPct", 30.0)
            .Build();

        Rule.Check(s, AnyProfile).Should().ContainSingle()
            .Which.Severity.Should().Be("error");
    }

    [Fact]
    public void Check_MissingBusLoad_NoIssue()
    {
        Rule.Check(new ScenarioBuilder().Build(), AnyProfile).Should().BeEmpty();
    }
}
