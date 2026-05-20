using TestGen.Core.Models;

namespace TestGen.Core.Validation;

public interface IValidationRule
{
    IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile);
}
