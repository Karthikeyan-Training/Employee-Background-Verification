namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;

public interface IDocumentExtractionService
{
    Task<DocumentDetails> ExtractAsync(string ocrText);
}
