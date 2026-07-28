namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IModelEvaluationService
{
    /// <summary>
    /// Runs the full evaluation suite against all configured models and returns a
    /// ranked comparison report with a Markdown table and recommendation.
    /// </summary>
    Task<ModelComparisonReport> EvaluateModelsAsync();
}
