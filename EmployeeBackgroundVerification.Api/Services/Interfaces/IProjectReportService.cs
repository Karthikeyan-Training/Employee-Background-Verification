namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IProjectReportService
{
    /// <summary>
    /// Generates the full project Markdown report (Problem Statement, Architecture,
    /// Dataset Source, Model Comparison, Model Selection Justification, Prompt
    /// Engineering Results, Demo Results, Limitations, Future Enhancements),
    /// saves it to the configured Reports folder, and returns the result.
    /// </summary>
    Task<ProjectReportResult> GenerateAndSaveAsync(
        ModelComparisonReport? modelComparison = null,
        PromptExperimentReport? promptExperiment = null,
        CancellationToken cancellationToken = default);
}
