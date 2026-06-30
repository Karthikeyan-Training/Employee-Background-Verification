namespace EmployeeBackgroundVerification.Api.Services;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class ReportService : IReportService
{
    public Task<ReportResult> GenerateReportAsync(BackgroundVerificationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var reportContent = $"Candidate: {result.CandidateName}\n" +
                            $"Email: {result.Email}\n" +
                            $"Verification Level: {result.VerificationLevel}\n" +
                            $"Status: {result.Status}\n" +
                            $"Summary: {result.Summary}\n" +
                            $"Completed On: {result.CompletedOn:O}\n";

        var report = new ReportResult
        {
            ReportId = Guid.NewGuid().ToString("D"),
            Content = reportContent
        };

        return Task.FromResult(report);
    }
}
