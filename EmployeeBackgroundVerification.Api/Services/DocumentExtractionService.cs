namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class DocumentExtractionService : IDocumentExtractionService
{
    private readonly IOllamaService _ollama;
    private readonly ILogger<DocumentExtractionService> _logger;

    public DocumentExtractionService(IOllamaService ollama, ILogger<DocumentExtractionService> logger)
    {
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentDetails> ExtractAsync(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            throw new ArgumentException("OCR text is required.", nameof(ocrText));
        }

        var prompt = BuildPrompt(ocrText);
        string response;
        try
        {
            response = await _ollama.GenerateAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama call failed while extracting document details.");
            // fall back to heuristics using OCR text
            return HeuristicExtract(ocrText);
        }

        // Try to parse response as JSON
        var docDetails = TryParseJsonResponse(response);
        if (docDetails is not null)
        {
            return docDetails;
        }

        _logger.LogWarning("Ollama response could not be parsed as JSON, attempting to recover." );

        // Try to extract JSON substring from the response
        var jsonSnippet = ExtractJsonBlock(response);
        if (jsonSnippet is not null)
        {
            docDetails = TryParseJsonResponse(jsonSnippet);
            if (docDetails is not null)
            {
                return docDetails;
            }
        }

        // Last-resort heuristics from the OCR text
        _logger.LogInformation("Falling back to heuristic extraction from OCR text.");
        return HeuristicExtract(ocrText);
    }

    private static string BuildPrompt(string ocrText)
    {
        // Instruct model to return valid JSON only with the specified keys.
        return $"Extract the following fields from the provided document text and return valid JSON only (no commentary). " +
               "Fields: fullName, dateOfBirth (ISO yyyy-MM-dd if possible), address, aadhaarNumber, panNumber, degree, university, companyName. " +
               "If a field is not present, return an empty string for that field. " +
               "Document text:\n" + ocrText;
    }

    private DocumentDetails? TryParseJsonResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = doc.RootElement;
            var result = new DocumentDetails
            {
                FullName = GetString(root, "fullName"),
                DateOfBirth = GetString(root, "dateOfBirth"),
                Address = GetString(root, "address"),
                AadhaarNumber = GetString(root, "aadhaarNumber"),
                PanNumber = GetString(root, "panNumber"),
                Degree = GetString(root, "degree"),
                University = GetString(root, "university"),
                CompanyName = GetString(root, "companyName"),
            };

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "JSON parse failed.");
            return null;
        }
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? ExtractJsonBlock(string text)
    {
        // Simple heuristic: find first '{' and last '}' and take substring
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }

        return null;
    }

    private DocumentDetails HeuristicExtract(string text)
    {
        var details = new DocumentDetails
        {
            AadhaarNumber = ExtractAadhaar(text),
            PanNumber = ExtractPan(text),
            DateOfBirth = ExtractDate(text) ?? string.Empty,
            FullName = ExtractName(text),
            Address = ExtractAddress(text),
            Degree = ExtractDegree(text),
            University = ExtractUniversity(text),
            CompanyName = ExtractCompany(text)
        };

        return details;
    }

    private static string ExtractAadhaar(string text)
    {
        // Aadhaar: 12 digits, possibly grouped
        var m = Regex.Match(text, @"\b(\d{4}\s?\d{4}\s?\d{4})\b");
        return m.Success ? Regex.Replace(m.Groups[1].Value, @"\s+", string.Empty) : string.Empty;
    }

    private static string ExtractPan(string text)
    {
        var m = Regex.Match(text.ToUpperInvariant(), @"\b([A-Z]{5}[0-9]{4}[A-Z])\b");
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private static string? ExtractDate(string text)
    {
        // Look for common date formats
        var patterns = new[]
        {
            "(\\d{2}/\\d{2}/\\d{4})",
            "(\\d{2}-\\d{2}-\\d{4})",
            "(\\d{4}-\\d{2}-\\d{2})"
        };

        foreach (var p in patterns)
        {
            var m = Regex.Match(text, p);
            if (m.Success)
            {
                if (DateTime.TryParse(m.Groups[1].Value, out var dt))
                {
                    return dt.ToString("yyyy-MM-dd");
                }
            }
        }

        return null;
    }

    private static string ExtractName(string text)
    {
        // Heuristic: first non-empty line with letters (avoid lines containing 'DOB' 'Date' 'PAN' etc.)
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var low = trimmed.ToLowerInvariant();
            if (low.Contains("dob") || low.Contains("date") || low.Contains("pan") || low.Contains("aadhaar") || low.Contains("name:") )
            {
                // try to capture after colon
                var idx = trimmed.IndexOf(':');
                if (idx >= 0 && idx + 1 < trimmed.Length)
                {
                    var candidate = trimmed.Substring(idx + 1).Trim();
                    if (candidate.Length > 1) return candidate;
                }
                continue;
            }

            // Prefer lines with letters and at least one space (first + last name)
            if (Regex.IsMatch(trimmed, "[A-Za-z]") && trimmed.Contains(' '))
            {
                return trimmed;
            }
        }

        return string.Empty;
    }

    private static string ExtractAddress(string text)
    {
        // Try to find 'Address' label
        var m = Regex.Match(text, @"(?i)address[:\-]?\s*(.+?)(?:\r?\n\r?\n|\r?\n[A-Z]|$)", RegexOptions.Singleline);
        if (m.Success) return m.Groups[1].Value.Trim();

        // fallback: take a chunk of text
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 3)
        {
            return string.Join(", ", lines, 1, Math.Min(3, lines.Length - 1)).Trim();
        }

        return string.Empty;
    }

    private static string ExtractDegree(string text)
    {
        var m = Regex.Match(text, @"(?i)(Bachelor|B\.?Sc|BTech|BE|Master|M\.?Sc|MBA|B\.?Com|PhD|Doctorate)[^,\n\r]*");
        return m.Success ? m.Value.Trim() : string.Empty;
    }

    private static string ExtractUniversity(string text)
    {
        var m = Regex.Match(text, @"(?i)(University|Institute|College) of? [A-Za-z0-9 .,&-]+", RegexOptions.Singleline);
        return m.Success ? m.Value.Trim() : string.Empty;
    }

    private static string ExtractCompany(string text)
    {
        var m = Regex.Match(text, @"(?i)(Company|Employer|Organization|Worked at|Experience|Company:)[:\s-]*([A-Za-z0-9 &.,-]+)");
        if (m.Success)
        {
            return (m.Groups.Count >= 3 ? m.Groups[2].Value : m.Groups[0].Value).Trim();
        }

        // Fallback: look for typical resume header lines (first few lines)
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < Math.Min(10, lines.Length); i++)
        {
            var l = lines[i];
            if (l.IndexOf("Inc", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("LLP", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("Pvt", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("Limited", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return l.Trim();
            }
        }

        return string.Empty;
    }
}
