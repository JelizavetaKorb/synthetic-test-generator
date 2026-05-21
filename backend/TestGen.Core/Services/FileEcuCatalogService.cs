using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TestGen.Core.Models;

namespace TestGen.Core.Services;

public sealed class FileEcuCatalogService : IEcuCatalogService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly IReadOnlyList<EcuCatalogEntry> _entries;
    private readonly ILogger<FileEcuCatalogService> _logger;

    public FileEcuCatalogService(ILogger<FileEcuCatalogService> logger)
    {
        _logger = logger;

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                          ?? AppContext.BaseDirectory;
        var catalogPath = Path.Combine(assemblyDir, "Resources", "Profiles", "EcuCatalog.json");

        if (!File.Exists(catalogPath))
        {
            _entries = Array.Empty<EcuCatalogEntry>();
            return;
        }

        try
        {
            var json    = File.ReadAllText(catalogPath);
            var entries = JsonSerializer.Deserialize<EcuCatalogEntry[]>(json, JsonOpts);
            _entries = (entries ?? Array.Empty<EcuCatalogEntry>())
                       .Where(e => !string.IsNullOrWhiteSpace(e.EcuId))
                       .ToList()
                       .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load ECU catalog from '{Path}'.", catalogPath);
            _entries = Array.Empty<EcuCatalogEntry>();
        }
    }

    public IReadOnlyList<EcuCatalogEntry> GetAll() => _entries;

    public EcuCatalogEntry? GetById(string ecuId) =>
        _entries.FirstOrDefault(e => e.EcuId.Equals(ecuId, StringComparison.OrdinalIgnoreCase));
}
