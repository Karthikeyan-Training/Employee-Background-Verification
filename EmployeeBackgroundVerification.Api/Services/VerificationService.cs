namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class VerificationService : IVerificationService
{
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(ILogger<VerificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<VerificationResult> VerifyAsync(IEnumerable<DocumentSource> documents)
    {
        if (documents is null) throw new ArgumentNullException(nameof(documents));

        var docs = documents.ToList();
        var result = new VerificationResult();

        // Fields to verify
        var fields = new[] { "FullName", "DateOfBirth", "Address", "AadhaarNumber", "PanNumber" };

        foreach (var field in fields)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var doc in docs)
            {
                var value = GetFieldValue(doc.Details, field);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values[doc.SourceName ?? string.Empty] = value;
                }
            }

            if (values.Count == 0)
            {
                result.MissingFields.Add(field);
                continue;
            }

            // Normalize values for comparison
            var normalized = values.Values
                .Select(v => NormalizeField(field, v))
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count == 1)
            {
                result.MatchedFields.Add(field);
            }
            else
            {
                // Build mismatched comparison
                var cmp = new FieldComparison { FieldName = field, IsMatch = false };
                foreach (var kv in values)
                {
                    cmp.ValuesBySource[kv.Key] = kv.Value;
                }
                result.MismatchedFields.Add(cmp);
            }

            // Add format warnings for identifiers
            if (field == "AadhaarNumber")
            {
                foreach (var v in values.Values)
                {
                    if (!Regex.IsMatch(Regex.Replace(v, "\\s+", ""), "^\\d{12}$"))
                    {
                        result.Warnings.Add($"Aadhaar value '{v}' has unexpected format.");
                    }
                }
            }

            if (field == "PanNumber")
            {
                foreach (var v in values.Values)
                {
                    if (!Regex.IsMatch(v.ToUpperInvariant(), "^[A-Z]{5}[0-9]{4}[A-Z]$"))
                    {
                        result.Warnings.Add($"PAN value '{v}' has unexpected format.");
                    }
                }
            }
        }

        // Determine overall status
        if (result.MissingFields.Count == 0 && result.MismatchedFields.Count == 0)
        {
            result.Status = VerificationStatus.Verified;
        }
        else if (result.MatchedFields.Count > 0 || result.MismatchedFields.Count > 0)
        {
            result.Status = VerificationStatus.Partial;
        }
        else
        {
            result.Status = VerificationStatus.Unverified;
        }

        return Task.FromResult(result);
    }

    private static string GetFieldValue(DocumentDetails d, string field)
    {
        return field switch
        {
            "FullName" => d.FullName ?? string.Empty,
            "DateOfBirth" => d.DateOfBirth ?? string.Empty,
            "Address" => d.Address ?? string.Empty,
            "AadhaarNumber" => d.AadhaarNumber ?? string.Empty,
            "PanNumber" => d.PanNumber ?? string.Empty,
            _ => string.Empty
        };
    }

    private static string NormalizeField(string field, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        value = value.Trim();
        if (field == "AadhaarNumber")
        {
            return Regex.Replace(value, "\\s+", string.Empty);
        }
        if (field == "PanNumber")
        {
            return value.ToUpperInvariant();
        }
        if (field == "DateOfBirth")
        {
            // attempt to normalize to yyyy-MM-dd
            if (DateTime.TryParse(value, out var dt))
            {
                return dt.ToString("yyyy-MM-dd");
            }
            return value;
        }

        // For names/addresses, normalize whitespace and case
        return Regex.Replace(value.ToUpperInvariant(), "\\s+", " ");
    }
}
