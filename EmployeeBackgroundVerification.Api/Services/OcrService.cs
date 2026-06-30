namespace EmployeeBackgroundVerification.Api.Services;

using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class OcrService : IOcrService
{
    private const string SampleExtractedText = "Sample extracted text from the uploaded document. " +
                                               "This mock implementation preserves the contract for future Azure Document Intelligence integration.";

    public Task<string> ExtractTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new System.ArgumentException("File path is required.", nameof(filePath));
        }

        // This is a mock implementation. Replace with Azure Document Intelligence logic later.
        return Task.FromResult(SampleExtractedText);
    }
}
