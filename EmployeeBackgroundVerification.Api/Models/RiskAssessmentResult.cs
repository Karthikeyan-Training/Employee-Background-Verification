namespace EmployeeBackgroundVerification.Api.Models;

public sealed class RiskAssessmentResult
{
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}
