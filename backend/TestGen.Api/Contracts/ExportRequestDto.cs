using System.Text.Json;

namespace TestGen.Api.Contracts;

public sealed class ExportRequestDto
{
    public JsonElement[] Scenarios { get; set; } = Array.Empty<JsonElement>();
    public string Format { get; set; } = "json";
}
