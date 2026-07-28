namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IReportGenerationService
{
    /// <summary>
    /// Generates a professional Markdown background verification report using Ollama.
    /// </summary>
    /// <param name="request">Original verification request containing employee details.</param>
    /// <param name="verificationResult">Field-level verification outcome.</param>
    /// <param name="fraudResult">Fraud indicators detected during analysis.</param>
    /// <param name="riskScore">Computed numeric risk score (0–100).</param>
    /// <param name="riskLevel">Human-readable risk level (e.g. Low, Medium, High, Critical).</param>
    /// <returns>A <see cref="ReportResult"/> whose <c>Content</c> is a Markdown document.</returns>
    Task<ReportResult> GenerateAsync(
        BackgroundVerificationRequest request,
        VerificationResult verificationResult,
        FraudResult fraudResult,
        int riskScore,
        string riskLevel);
}
