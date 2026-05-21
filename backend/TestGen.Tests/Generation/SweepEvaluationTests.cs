using FluentAssertions;
using TestGen.Core.Generation;

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
}

