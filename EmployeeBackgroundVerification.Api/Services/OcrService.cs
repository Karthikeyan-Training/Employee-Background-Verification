namespace EmployeeBackgroundVerification.Api.Services;

using System.IO;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class OcrService : IOcrService
{
    public Task<string> ExtractTextAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new System.ArgumentException("File path is required.", nameof(filePath));
        }

        // For .txt files, read content directly — used by test documents and demo scenarios.
        // For all other types (PDF, JPG, PNG), this is a mock that simulates OCR output.
        // Replace the non-txt branch with Azure Document Intelligence logic for production.
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".txt" && File.Exists(filePath))
        {
            return File.ReadAllTextAsync(filePath);
        }

        // Mock OCR for binary formats — preserves the contract for future real OCR integration.
        return Task.FromResult(
            $"Sample extracted text from the uploaded document '{Path.GetFileName(filePath)}'. " +
            "This mock implementation preserves the contract for future Azure Document Intelligence integration.");
    }
}
