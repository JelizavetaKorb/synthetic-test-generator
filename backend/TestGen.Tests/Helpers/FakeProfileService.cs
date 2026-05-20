using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Tests.Helpers;

internal sealed class FakeProfileService : IProfileService
{
    private readonly Profile[] _profiles;

    public FakeProfileService(params Profile[] profiles) => _profiles = profiles;

    public IReadOnlyList<Profile> GetAll() => _profiles;

    public Profile? GetById(string profileId) =>
        _profiles.FirstOrDefault(p =>
            p.ProfileId.Equals(profileId, StringComparison.OrdinalIgnoreCase));
}

