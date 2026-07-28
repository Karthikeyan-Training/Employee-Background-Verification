namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IPromptExperimentService
{
    /// <summary>
    /// Runs the same employee document through Zero-shot, Few-shot, and
    /// Chain-of-Thought prompting strategies and returns a comparison report.
    /// </summary>
    /// <param name="document">
    /// The employee document details to use as the subject of every experiment.
    /// </param>
    Task<PromptExperimentReport> RunExperimentsAsync(DocumentDetails document);
}
