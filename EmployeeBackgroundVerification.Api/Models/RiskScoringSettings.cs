namespace EmployeeBackgroundVerification.Api.Models;

public sealed class RiskScoringSettings
{
    public int NameMismatchWeight { get; init; } = 30;
    public int DobMismatchWeight { get; init; } = 30;
    public int MissingPanWeight { get; init; } = 20;
    public int MissingAadhaarWeight { get; init; } = 20;
    public int FraudDetectedWeight { get; init; } = 40;
}
