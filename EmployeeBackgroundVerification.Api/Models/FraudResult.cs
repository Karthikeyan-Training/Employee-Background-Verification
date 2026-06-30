using System.Collections.Generic;

namespace EmployeeBackgroundVerification.Api.Models;

public enum FraudSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class FraudIndicator
{
    public string IndicatorName { get; set; } = string.Empty;
    public FraudSeverity Severity { get; set; } = FraudSeverity.Low;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

public class FraudResult
{
    public bool IsFraudulent { get; set; }
    public FraudSeverity MaxSeverity { get; set; } = FraudSeverity.Low;
    public List<FraudIndicator> Indicators { get; set; } = new();
    public List<string> ManualReviewRecommendations { get; set; } = new();
}
