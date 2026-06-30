namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IBackgroundVerificationService
{
    Task<BackgroundVerificationResult> VerifyAsync(BackgroundVerificationRequest request);
}
