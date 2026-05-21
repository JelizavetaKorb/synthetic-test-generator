using System.Text;
using System.Text.Json;
using FluentAssertions;
using TestGen.Core.Formatting;

namespace TestGen.Tests.Formatting;

public sealed class CsvExportTests
{
    private static readonly CsvScenarioExporter Exporter = new();

    private static JsonElement MakeScenario(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Export_CsvFormat_ReturnsCsvWithHeaders()
    {
        var scenario = MakeScenario("{\"scenario_id\":\"s1\",\"name\":\"Test\"}");

        var bytes = Exporter.ExportAsCsv(new[] { scenario });
        var csv   = Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("scenario_id");
        csv.Should().Contain("name");
        csv.Should().Contain("s1");
        csv.Should().Contain("Test");
    }

    [Fact]
    public void Export_CsvFormat_NestedFieldsFlattened()
    {
        var scenario = MakeScenario("{\"initial_conditions\":{\"ambient_temperature_C\":25}}");

        var bytes = Exporter.ExportAsCsv(new[] { scenario });
        var csv   = Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("initial_conditions.ambient_temperature_C");
        csv.Should().Contain("25");
    }

    [Fact]
    public void Export_CsvFormat_MultipleScenarios_AllRowsPresent()
    {
        var s1 = MakeScenario("{\"scenario_id\":\"s1\",\"name\":\"First\"}");
        var s2 = MakeScenario("{\"scenario_id\":\"s2\",\"name\":\"Second\"}");

        var bytes = Exporter.ExportAsCsv(new[] { s1, s2 });
        var csv   = Encoding.UTF8.GetString(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3);
    }
}
