namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient httpClient, IOptions<OllamaSettings> options, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(prompt));
        }

        var payload = new
        {
            model = _settings.ModelName,
            prompt = prompt
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        int attempt = 0;
        while (true)
        {
            attempt++;
            try
            {
                var response = await _httpClient.PostAsync("/api/generate", content);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Ollama responded with non-success status {Status}: {Error}", response.StatusCode, err);
                    throw new HttpRequestException($"Ollama API returned {response.StatusCode}: {err}");
                }

                var responseText = await response.Content.ReadAsStringAsync();

                // Try to parse common JSON shapes, fall back to raw text.
                try
                {
                    using var doc = JsonDocument.Parse(responseText);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("completion", out var completion))
                        {
                            return completion.GetString() ?? string.Empty;
                        }

                        if (doc.RootElement.TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? string.Empty;
                        }

                        if (doc.RootElement.TryGetProperty("output", out var output))
                        {
                            return output.GetString() ?? string.Empty;
                        }
                    }
                }
                catch (JsonException)
                {
                    // ignore and return raw
                }

                return responseText;
            }
            catch (Exception ex) when (attempt <= _settings.MaxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed to call Ollama, retrying...", attempt);
                await Task.Delay(_settings.RetryDelayMs * attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Ollama after {Attempts} attempts.", attempt);
                throw;
            }
        }
    }
}
