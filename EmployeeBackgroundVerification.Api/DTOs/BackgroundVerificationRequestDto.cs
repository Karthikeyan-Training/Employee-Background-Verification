namespace EmployeeBackgroundVerification.Api.DTOs;

public sealed class BackgroundVerificationRequestDto
{
    public string CandidateName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool CriminalRecordCheck { get; set; }
}
