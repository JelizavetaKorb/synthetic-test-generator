using System.Text.Json;

namespace TestGen.Core.Formatting;

public interface IScenarioExporter
{
    byte[] ExportAsCsv(IReadOnlyList<JsonElement> scenarios);
}

