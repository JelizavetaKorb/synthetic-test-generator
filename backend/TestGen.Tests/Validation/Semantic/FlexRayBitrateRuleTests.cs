using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Semantic;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Semantic;

public sealed class FlexRayBitrateRuleTests
{
    private static readonly FlexRayBitrateRule Rule       = new();
    private static readonly Profile            AnyProfile = new ProfileBuilder().Build();

    [Theory]
    [InlineData(2500.0)]
    [InlineData(5000.0)]
    [InlineData(10000.0)]
    public void Check_AllowedRate_NoIssue(double rate)
    {
        var s = new ScenarioBuilder()
            .WithParameter("bus.busType",    "FlexRay")
            .WithParameter("bus.bitrateKbps", rate)
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }

    [Theory]
    [InlineData(500.0)]
    [InlineData(1000.0)]
    [InlineData(8000.0)]
    public void Check_InvalidRate_Error(double rate)
    {
        var s = new ScenarioBuilder()
            .WithParameter("bus.busType",    "FlexRay")
            .WithParameter("bus.bitrateKbps", rate)
            .Build();

        Rule.Check(s, AnyProfile).Should().ContainSingle()
            .Which.Severity.Should().Be("error");
    }

    [Fact]
    public void Check_NonFlexRay_NoIssue()
    {
        var s = new ScenarioBuilder()
            .WithParameter("bus.busType",    "CAN")
            .WithParameter("bus.bitrateKbps", 500.0)
            .Build();

        Rule.Check(s, AnyProfile).Should().BeEmpty();
    }
}
