using System.Collections.Generic;

namespace EmployeeBackgroundVerification.Api.Models;

public enum VerificationStatus
{
    Unverified,
    Partial,
    Verified
}

public class FieldComparison
{
    public string FieldName { get; set; } = string.Empty;
    public bool IsMatch { get; set; }
    public Dictionary<string, string> ValuesBySource { get; set; } = new();
}

public class VerificationResult
{
    public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;
    public List<string> MatchedFields { get; set; } = new();
    public List<FieldComparison> MismatchedFields { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> MissingFields { get; set; } = new();
}
