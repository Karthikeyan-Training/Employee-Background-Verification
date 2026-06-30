namespace EmployeeBackgroundVerification.Api.Helpers;

using EmployeeBackgroundVerification.Api.DTOs;
using EmployeeBackgroundVerification.Api.Models;

public static class MappingExtensions
{
    public static BackgroundVerificationRequest ToDomain(this BackgroundVerificationRequestDto request)
    {
        return new BackgroundVerificationRequest
        {
            CandidateName = request.CandidateName,
            Email = request.Email,
            Position = request.Position,
            CriminalRecordCheck = request.CriminalRecordCheck
        };
    }

    public static BackgroundVerificationResponseDto ToDto(this BackgroundVerificationResult result)
    {
        return new BackgroundVerificationResponseDto
        {
            CandidateName = result.CandidateName,
            Email = result.Email,
            VerificationLevel = result.VerificationLevel,
            Status = result.Status,
            Summary = result.Summary,
            CompletedOn = result.CompletedOn
        };
    }
}
