using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

public interface IVerificationService
{
    Task<VerificationResult> VerifyAsync(IEnumerable<DocumentSource> documents);
}
