namespace EmployeeBackgroundVerification.Api.Models;

public sealed class BackgroundVerificationSettings
{
    public string DefaultCheckLevel { get; init; } = "Standard";
    public string ReportPath { get; init; } = "Reports";
}
