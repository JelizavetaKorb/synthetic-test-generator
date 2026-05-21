using TestGen.Core.Models;

namespace TestGen.Tests.Helpers;

internal sealed class ProfileBuilder
{
    private string _id = "test-profile";
    private readonly Dictionary<string, object> _defaults    = new();
    private readonly List<string>               _locked      = new();
    private readonly List<string>               _assertions  = new();

    public ProfileBuilder WithId(string id)                      { _id = id; return this; }
    public ProfileBuilder WithDefault(string key, object value)  { _defaults[key] = value; return this; }
    public ProfileBuilder WithLocked(string key)                 { _locked.Add(key); return this; }
    public ProfileBuilder WithAssertion(string assertion)        { _assertions.Add(assertion); return this; }

    public Profile Build() => new()
    {
        ProfileId             = _id,
        Name                  = _id,
        DefaultParameters     = new Dictionary<string, object>(_defaults),
        LockedParameters      = _locked.ToArray(),
        RecommendedAssertions = _assertions.ToArray()
    };
}
