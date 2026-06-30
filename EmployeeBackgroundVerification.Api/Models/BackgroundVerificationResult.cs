namespace EmployeeBackgroundVerification.Api.Models;

using System;

public sealed class BackgroundVerificationResult
{
    public string CandidateName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string VerificationLevel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime CompletedOn { get; init; }
}
