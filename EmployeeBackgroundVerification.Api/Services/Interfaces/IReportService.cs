namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IReportService
{
    Task<ReportResult> GenerateReportAsync(BackgroundVerificationResult result);
}
