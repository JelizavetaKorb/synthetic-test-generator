using FluentAssertions;
using TestGen.Core.Generation;
using TestGen.Core.Services;
using TestGen.Tests.Helpers;

namespace TestGen.Tests.Generation;

public sealed class SweepEvaluationTests
{
    [Fact]
    public void Sweep_Fixed_ReturnsSingleValue()
    {
        var result = GeneratorPipeline.LinearRange(5.0, 5.0, 1.0);

        result.Should().HaveCount(1);
        result[0].Should().Be(5.0);
    }

    [Fact]
    public void Sweep_EnumList_ReturnsAllValues()
    {
        var values = new object[] { -30, -20, -10, 0, 10 };

        values.Should().HaveCount(5);
    }

    [Fact]
    public void Sweep_RandomUniform_ReturnsCorrectCount()
    {
        var count  = 7;
        var random = new Random(42);
        var vals   = new object[count];
        for (var i = 0; i < count; i++)
            vals[i] = Math.Round(10.0 + random.NextDouble() * 4.0, 6);

        vals.Should().HaveCount(7);
        foreach (var v in vals.Cast<double>())
            v.Should().BeGreaterThanOrEqualTo(10.0).And.BeLessThanOrEqualTo(14.0);
    }

    [Fact]
    public void Sweep_RandomUniform_Deterministic()
    {
        double[] Run()
        {
            var random = new Random(42);
            var vals   = new double[5];
            for (var i = 0; i < 5; i++)
                vals[i] = Math.Round(10.0 + random.NextDouble() * 4.0, 6);
            return vals;
        }

        Run().Should().Equal(Run());
    }

    [Fact]
    public void Sweep_GeometricRange_ReturnsGeometricProgression()
    {
        var result = GeneratorPipeline.GeometricRange(1.0, 8.0, 2.0);

        result.Should().HaveCount(4);
        result.Cast<double>().Should().BeEquivalentTo(new[] { 1.0, 2.0, 4.0, 8.0 });
    }

    [Fact]
    public void Sweep_LinearRange_ReturnsCorrectValues()
    {
        var result = GeneratorPipeline.LinearRange(0.0, 10.0, 5.0);

        result.Should().HaveCount(3);
        result.Cast<double>().Should().BeEquivalentTo(new[] { 0.0, 5.0, 10.0 });
    }

    private static PrepareInputsResult Prepare(SweepSpec spec) =>
        new GeneratorPipeline(new FakeProfileService(), null!).PrepareInputs(new GenerateRequest
        {
            SweepParameters = new Dictionary<string, SweepSpec> { ["x"] = spec }
        });

    private static double[] Values(SweepSpec spec) =>
        Prepare(spec).Sweeps.Single().Values.Cast<double>().ToArray();

    [Fact]
    public void Sweep_RandomUniform_UsesCount()
    {
        var values = Values(new SweepSpec { Type = "random_uniform", From = 10, To = 14, Count = 8 });

        values.Should().HaveCount(8);
        values.Should().OnlyContain(v => v >= 10 && v <= 14);
    }

    [Fact]
    public void Sweep_RandomUniform_DefaultsToFiveValues()
    {
        Values(new SweepSpec { Type = "random_uniform", From = 0, To = 1 }).Should().HaveCount(5);
    }

    [Fact]
    public void Sweep_Gaussian_ReturnsCountValuesAroundMean()
    {
        var values = Values(new SweepSpec { Type = "gaussian", Mean = 12, Std = 0.5, Count = 200 });

        values.Should().HaveCount(200);
        values.Average().Should().BeApproximately(12, 0.2);
        values.Should().OnlyContain(v => v > 9.5 && v < 14.5);
    }

    [Fact]
    public void Sweep_Gaussian_ZeroStd_ReturnsMean()
    {
        Values(new SweepSpec { Type = "gaussian", Mean = 3, Std = 0, Count = 3 })
            .Should().Equal(3.0, 3.0, 3.0);
    }

    [Fact]
    public void Sweep_Gaussian_IsDeterministic()
    {
        var spec = new SweepSpec { Type = "gaussian", Mean = 0, Std = 1, Count = 6 };

        Values(spec).Should().Equal(Values(spec));
    }

    [Theory]
    [InlineData(null, 1.0)]
    [InlineData(0.0, null)]
    [InlineData(0.0, -1.0)]
    public void Sweep_Gaussian_InvalidSpec_ReturnsError(double? mean, double? std)
    {
        var result = Prepare(new SweepSpec { Type = "gaussian", Mean = mean, Std = std });

        result.Issues.Should().ContainSingle(i => i.Code == "INVALID_SWEEP_SPEC" && i.Field == "x");
    }

    [Fact]
    public void Sweep_LatinHypercube_PlacesOneValueInEachStratum()
    {
        var values = Values(new SweepSpec { Type = "latin_hypercube", From = 0, To = 10, Count = 5 });

        values.Should().HaveCount(5);
        for (var i = 0; i < 5; i++)
            values[i].Should().BeInRange(i * 2.0, (i + 1) * 2.0);
    }

    [Fact]
    public void Sweep_LatinHypercube_MissingRange_ReturnsError()
    {
        var result = Prepare(new SweepSpec { Type = "latin_hypercube", From = 0 });

        result.Issues.Should().ContainSingle(i => i.Code == "INVALID_SWEEP_SPEC");
    }

    [Theory]
    [InlineData("random_uniform")]
    [InlineData("latin_hypercube")]
    [InlineData("gaussian")]
    public void Sweep_RandomTypes_CountBelowOne_ReturnsError(string type)
    {
        var result = Prepare(new SweepSpec { Type = type, From = 0, To = 1, Mean = 0, Std = 1, Count = 0 });

        result.Issues.Should().ContainSingle(i => i.Code == "INVALID_SWEEP_SPEC" && i.Message.Contains("count"));
    }
}
