namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IFraudDetectionService
{
    Task<FraudResult> AnalyzeAsync(IEnumerable<DocumentSource> documents);
}
