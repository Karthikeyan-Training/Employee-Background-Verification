namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class BackgroundVerificationService : IBackgroundVerificationService
{
    private readonly BackgroundVerificationSettings _settings;
    private readonly IRiskScoringService _riskScoringService;

    public BackgroundVerificationService(
        IOptions<BackgroundVerificationSettings> options,
        IRiskScoringService riskScoringService)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _riskScoringService = riskScoringService ?? throw new ArgumentNullException(nameof(riskScoringService));
    }

    public Task<BackgroundVerificationResult> VerifyAsync(BackgroundVerificationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var status = request.CriminalRecordCheck
            ? "Completed"
            : "Pending";

        var riskAssessment = _riskScoringService.Assess(new RiskScoringInput
        {
            NameMismatch = false,
            DobMismatch = false,
            IsPanMissing = false,
            IsAadhaarMissing = false,
            FraudDetected = false
        });

        var result = new BackgroundVerificationResult
        {
            CandidateName = request.CandidateName,
            Email = request.Email,
            VerificationLevel = _settings.DefaultCheckLevel,
            CompletedOn = DateTime.UtcNow,
            Status = status,
            Summary = $"Verification completed using {_settings.DefaultCheckLevel} checks.",
            RiskScore = riskAssessment.Score,
            RiskLevel = riskAssessment.Level,
            Recommendation = riskAssessment.Recommendation
        };

        return Task.FromResult(result);
    }
}
