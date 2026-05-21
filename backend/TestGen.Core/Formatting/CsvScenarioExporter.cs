using System.Text;
using System.Text.Json;

namespace TestGen.Core.Formatting;

public sealed class CsvScenarioExporter : IScenarioExporter
{
    public byte[] ExportAsCsv(IReadOnlyList<JsonElement> scenarios)
    {
        var rows    = scenarios.Select(s => FlattenElement(s, string.Empty)).ToList();
        var headers = rows.SelectMany(r => r.Keys).Distinct().OrderBy(k => k).ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            var values = headers.Select(h => row.TryGetValue(h, out var val) ? EscapeCsv(val) : string.Empty);
            sb.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static Dictionary<string, string> FlattenElement(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, string>();
        FlattenInto(element, prefix, result);
        return result;
    }

    private static void FlattenInto(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenInto(prop.Value, key, result);
                }
                break;

            case JsonValueKind.Array:
                var idx = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenInto(item, $"{prefix}.{idx}", result);
                    idx++;
                }
                break;

            default:
                result[prefix] = element.ToString();
                break;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

