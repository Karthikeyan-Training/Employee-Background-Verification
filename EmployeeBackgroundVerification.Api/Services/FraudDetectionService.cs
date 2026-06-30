namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly FraudDetectionSettings _settings;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(IOptions<FraudDetectionSettings> options, ILogger<FraudDetectionService> logger)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<FraudResult> AnalyzeAsync(IEnumerable<DocumentSource> documents)
    {
        if (documents is null) throw new ArgumentNullException(nameof(documents));

        var docs = documents.ToList();
        var result = new FraudResult();

        // Rule 1: Check for different names
        CheckDifferentNames(docs, result);

        // Rule 2: Check for different DOB
        CheckDifferentDOB(docs, result);

        // Rule 3: Check for suspicious document numbers
        CheckSuspiciousDocumentNumbers(docs, result);

        // Rule 4: Check for missing mandatory documents
        CheckMissingMandatoryDocuments(docs, result);

        // Rule 5: Check for duplicate document numbers
        CheckDuplicateDocumentNumbers(docs, result);

        // Determine overall fraud status and max severity
        result.IsFraudulent = result.Indicators.Any(i => i.Severity >= FraudSeverity.High);
        result.MaxSeverity = result.Indicators.Count > 0
            ? result.Indicators.Max(i => i.Severity)
            : FraudSeverity.Low;

        // Add manual review recommendations based on severity
        AddManualReviewRecommendations(result);

        _logger.LogInformation($"Fraud analysis complete: IsFraudulent={result.IsFraudulent}, Severity={result.MaxSeverity}, IndicatorCount={result.Indicators.Count}");

        return Task.FromResult(result);
    }

    private void CheckDifferentNames(List<DocumentSource> docs, FraudResult result)
    {
        var names = docs
            .Where(d => !string.IsNullOrWhiteSpace(d.Details.FullName))
            .Select(d => d.Details.FullName.ToUpperInvariant().Trim())
            .Distinct()
            .ToList();

        if (names.Count > 1)
        {
            var indicator = new FraudIndicator
            {
                IndicatorName = "Different Names",
                Severity = FraudSeverity.High,
                Description = $"Multiple different names found across documents: {string.Join(", ", names)}",
                Details = new Dictionary<string, object> { { "names", names } }
            };
            result.Indicators.Add(indicator);
            result.ManualReviewRecommendations.Add("Verify employee identity and confirm correct legal name.");
        }
    }

    private void CheckDifferentDOB(List<DocumentSource> docs, FraudResult result)
    {
        var dobs = docs
            .Where(d => !string.IsNullOrWhiteSpace(d.Details.DateOfBirth))
            .Select(d => d.Details.DateOfBirth)
            .Distinct()
            .ToList();

        if (dobs.Count > 1)
        {
            var indicator = new FraudIndicator
            {
                IndicatorName = "Different Date of Birth",
                Severity = FraudSeverity.Critical,
                Description = $"Multiple different dates of birth found: {string.Join(", ", dobs)}",
                Details = new Dictionary<string, object> { { "dobs", dobs } }
            };
            result.Indicators.Add(indicator);
            result.ManualReviewRecommendations.Add("Immediately review and verify correct date of birth. This is a critical discrepancy.");
        }
    }

    private void CheckSuspiciousDocumentNumbers(List<DocumentSource> docs, FraudResult result)
    {
        foreach (var doc in docs)
        {
            // Check Aadhaar
            if (!string.IsNullOrWhiteSpace(doc.Details.AadhaarNumber))
            {
                foreach (var pattern in _settings.SuspiciousAadhaarPatterns)
                {
                    if (Regex.IsMatch(doc.Details.AadhaarNumber, pattern, RegexOptions.IgnoreCase))
                    {
                        var indicator = new FraudIndicator
                        {
                            IndicatorName = "Suspicious Aadhaar Format",
                            Severity = FraudSeverity.Medium,
                            Description = $"Aadhaar number '{doc.Details.AadhaarNumber}' from document '{doc.SourceName}' matches suspicious pattern.",
                            Details = new Dictionary<string, object>
                            {
                                { "aadhaar", doc.Details.AadhaarNumber },
                                { "source", doc.SourceName },
                                { "pattern", pattern }
                            }
                        };
                        result.Indicators.Add(indicator);
                        break;
                    }
                }
            }

            // Check PAN
            if (!string.IsNullOrWhiteSpace(doc.Details.PanNumber))
            {
                foreach (var pattern in _settings.SuspiciousPanPatterns)
                {
                    if (Regex.IsMatch(doc.Details.PanNumber, pattern, RegexOptions.IgnoreCase))
                    {
                        var indicator = new FraudIndicator
                        {
                            IndicatorName = "Suspicious PAN Format",
                            Severity = FraudSeverity.Medium,
                            Description = $"PAN '{doc.Details.PanNumber}' from document '{doc.SourceName}' matches suspicious pattern.",
                            Details = new Dictionary<string, object>
                            {
                                { "pan", doc.Details.PanNumber },
                                { "source", doc.SourceName },
                                { "pattern", pattern }
                            }
                        };
                        result.Indicators.Add(indicator);
                        break;
                    }
                }
            }
        }
    }

    private void CheckMissingMandatoryDocuments(List<DocumentSource> docs, FraudResult result)
    {
        var providedDocNames = docs.Select(d => d.SourceName.ToUpperInvariant()).ToList();

        foreach (var mandatory in _settings.MandatoryDocumentNames)
        {
            if (!providedDocNames.Contains(mandatory.ToUpperInvariant()))
            {
                var indicator = new FraudIndicator
                {
                    IndicatorName = "Missing Mandatory Document",
                    Severity = FraudSeverity.High,
                    Description = $"Mandatory document '{mandatory}' is missing.",
                    Details = new Dictionary<string, object> { { "documentName", mandatory } }
                };
                result.Indicators.Add(indicator);
                result.ManualReviewRecommendations.Add($"Request missing mandatory document: {mandatory}");
            }
        }
    }

    private void CheckDuplicateDocumentNumbers(List<DocumentSource> docs, FraudResult result)
    {
        // Check for duplicate Aadhaar
        var aadhaarCounts = docs
            .Where(d => !string.IsNullOrWhiteSpace(d.Details.AadhaarNumber))
            .GroupBy(d => d.Details.AadhaarNumber)
            .Where(g => g.Count() >= _settings.DuplicateDocumentNumberThreshold)
            .ToList();

        foreach (var group in aadhaarCounts)
        {
            if (group.Count() > 1)
            {
                var sources = string.Join(", ", group.Select(d => d.SourceName));
                var indicator = new FraudIndicator
                {
                    IndicatorName = "Duplicate Aadhaar",
                    Severity = FraudSeverity.Low,
                    Description = $"Aadhaar '{group.Key}' appears in multiple documents: {sources}",
                    Details = new Dictionary<string, object>
                    {
                        { "aadhaar", group.Key },
                        { "sources", group.Select(d => d.SourceName).ToList() },
                        { "count", group.Count() }
                    }
                };
                result.Indicators.Add(indicator);
            }
        }

        // Check for duplicate PAN
        var panCounts = docs
            .Where(d => !string.IsNullOrWhiteSpace(d.Details.PanNumber))
            .GroupBy(d => d.Details.PanNumber)
            .Where(g => g.Count() >= _settings.DuplicateDocumentNumberThreshold)
            .ToList();

        foreach (var group in panCounts)
        {
            if (group.Count() > 1)
            {
                var sources = string.Join(", ", group.Select(d => d.SourceName));
                var indicator = new FraudIndicator
                {
                    IndicatorName = "Duplicate PAN",
                    Severity = FraudSeverity.Low,
                    Description = $"PAN '{group.Key}' appears in multiple documents: {sources}",
                    Details = new Dictionary<string, object>
                    {
                        { "pan", group.Key },
                        { "sources", group.Select(d => d.SourceName).ToList() },
                        { "count", group.Count() }
                    }
                };
                result.Indicators.Add(indicator);
            }
        }
    }

    private void AddManualReviewRecommendations(FraudResult result)
    {
        if (result.MaxSeverity == FraudSeverity.Critical)
        {
            result.ManualReviewRecommendations.Insert(0, "CRITICAL: Escalate to compliance officer immediately.");
        }
        else if (result.MaxSeverity == FraudSeverity.High)
        {
            result.ManualReviewRecommendations.Insert(0, "HIGH PRIORITY: Requires manual verification before approval.");
        }
        else if (result.MaxSeverity == FraudSeverity.Medium && result.Indicators.Count > 0)
        {
            result.ManualReviewRecommendations.Insert(0, "Recommended to review document integrity and employee details.");
        }
    }
}
