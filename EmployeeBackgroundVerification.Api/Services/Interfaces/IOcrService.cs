namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;

public interface IOcrService
{
    Task<string> ExtractTextAsync(string filePath);
}
