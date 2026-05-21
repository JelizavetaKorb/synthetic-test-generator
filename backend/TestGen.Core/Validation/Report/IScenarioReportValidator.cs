using System.Text.Json;
namespace TestGen.Core.Validation.Report;
public interface IScenarioReportValidator
{
    ScenarioValidationReport Validate(JsonElement root);
}
