using TestGen.Core.Models;

namespace TestGen.Api.Contracts;

public sealed class ProfileDto
{
    public string ProfileId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> DefaultParameters { get; set; } = new();
    public string[] LockedParameters { get; set; } = Array.Empty<string>();
    public string[] RecommendedAssertions { get; set; } = Array.Empty<string>();

    public static ProfileDto FromDomain(Profile p) => new()
    {
        ProfileId = p.ProfileId,
        Name = p.Name,
        Description = p.Description,
        DefaultParameters = p.DefaultParameters,
        LockedParameters = p.LockedParameters,
        RecommendedAssertions = p.RecommendedAssertions
    };
}
