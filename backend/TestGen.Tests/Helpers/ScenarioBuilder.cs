using TestGen.Core.Models;

namespace TestGen.Tests.Helpers;

internal sealed class ScenarioBuilder
{
    private readonly Dictionary<string, object> _parameters = new();

    public ScenarioBuilder WithParameter(string key, object value)
    {
        _parameters[key] = value;
        return this;
    }

    public Scenario Build() => new()
    {
        Name       = "Test Scenario",
        Parameters = new Dictionary<string, object>(_parameters)
    };
}

