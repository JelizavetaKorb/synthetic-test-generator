using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Models;
using TestGen.Core.Validation.Safety;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Validation.Safety;

public sealed class AsilDFormalAssertionRuleTests
{
    private static readonly AsilDFormalAssertionRule Rule       = new();
    private static readonly Profile                  AnyProfile = new ProfileBuilder().Build();

    [Fact]
    public void Check_AsilDWithoutFormalAssertion_ReturnsError()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel",          "D")
            .WithParameter("assertions[0].type", "metric")
            .Build();

        var issues = Rule.Check(scenario, AnyProfile).ToList();

        issues.Should().ContainSingle();
        issues[0].Code.Should().Be(AsilDFormalAssertionRule.RuleCode);
        issues[0].Severity.Should().Be("error");
        issues[0].Field.Should().Be("asilLevel");
    }

    [Theory]
    [InlineData("STL")]
    [InlineData("LTL")]
    public void Check_AsilDWithFormalAssertion_ReturnsNoIssues(string type)
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel",          "D")
            .WithParameter("assertions[0].type", type)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }

    [Fact]
    public void Check_FormalAssertionFromProfileArray_ReturnsNoIssues()
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel",  "D")
            .WithParameter("assertions", JsonDocument.Parse("""[{"id":"a","type":"STL","expr":"x"}]""").RootElement)
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }

    [Theory]
    [InlineData("C")]
    [InlineData("QM")]
    public void Check_NotAsilD_ReturnsNoIssues(string asil)
    {
        var scenario = new ScenarioBuilder()
            .WithParameter("asilLevel",          asil)
            .WithParameter("assertions[0].type", "metric")
            .Build();

        Rule.Check(scenario, AnyProfile).Should().BeEmpty();
    }
}
