using TestGen.Core.Models;

namespace TestGen.Core.Services;

public interface IProfileService
{
    IReadOnlyList<Profile> GetAll();
    Profile? GetById(string profileId);
}
