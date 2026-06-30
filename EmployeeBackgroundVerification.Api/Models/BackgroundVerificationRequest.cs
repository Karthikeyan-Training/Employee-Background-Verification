namespace EmployeeBackgroundVerification.Api.Models;

public sealed class BackgroundVerificationRequest
{
    public string CandidateName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public bool CriminalRecordCheck { get; init; }
}
