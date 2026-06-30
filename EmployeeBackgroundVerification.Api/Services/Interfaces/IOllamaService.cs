namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Threading.Tasks;

public interface IOllamaService
{
    Task<string> GenerateAsync(string prompt);
}
