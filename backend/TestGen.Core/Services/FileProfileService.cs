using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TestGen.Core.Models;

namespace TestGen.Core.Services;

public sealed class FileProfileService : IProfileService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly IReadOnlyList<Profile> _profiles;
    private readonly ILogger<FileProfileService> _logger;

    public FileProfileService(ILogger<FileProfileService> logger)
    {
        _logger = logger;

        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                          ?? AppContext.BaseDirectory;
        var profilesDir = Path.Combine(assemblyDir, "Resources", "Profiles");

        if (!Directory.Exists(profilesDir))
        {
            _profiles = Array.Empty<Profile>();
            return;
        }

        var loaded = new List<Profile>();
        foreach (var file in Directory.EnumerateFiles(profilesDir, "*.json"))
        {

            var fileName = Path.GetFileName(file);
            if (fileName.Equals("EcuCatalog.json", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<Profile>(json, JsonOpts);
                if (profile is not null && !string.IsNullOrWhiteSpace(profile.ProfileId))
                    loaded.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile from '{File}'.", file);
            }
        }

        _profiles = loaded.AsReadOnly();
    }

    public IReadOnlyList<Profile> GetAll() => _profiles;

    public Profile? GetById(string profileId) =>
        _profiles.FirstOrDefault(p => p.ProfileId.Equals(profileId, StringComparison.OrdinalIgnoreCase));
}
