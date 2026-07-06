namespace EmployeeBackgroundVerification.Api.Models;

public sealed class RiskScoringInput
{
    public bool NameMismatch { get; init; }
    public bool DobMismatch { get; init; }
    public bool IsPanMissing { get; init; }
    public bool IsAadhaarMissing { get; init; }
    public bool FraudDetected { get; init; }
}
