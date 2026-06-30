namespace EmployeeBackgroundVerification.Api.Models;

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "llama2";
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 500;
}
