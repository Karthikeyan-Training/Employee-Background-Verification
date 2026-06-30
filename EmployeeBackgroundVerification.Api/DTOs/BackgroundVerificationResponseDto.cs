namespace EmployeeBackgroundVerification.Api.DTOs;

public sealed class BackgroundVerificationResponseDto
{
    public string CandidateName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string VerificationLevel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public DateTime CompletedOn { get; init; }
}
