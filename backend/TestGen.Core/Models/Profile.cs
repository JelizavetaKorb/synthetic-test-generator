namespace TestGen.Core.Models;

public sealed class Profile
{
    public string ProfileId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
    public string[] LockedParameters { get; set; } = Array.Empty<string>();
    public string[] RecommendedAssertions { get; set; } = Array.Empty<string>();
}
