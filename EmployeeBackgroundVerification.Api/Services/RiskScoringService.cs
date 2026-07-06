using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EmployeeBackgroundVerification.Api.Services;

public sealed class RiskScoringService : IRiskScoringService
{
    private readonly RiskScoringSettings _settings;

    public RiskScoringService(IOptions<RiskScoringSettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public RiskAssessmentResult Assess(RiskScoringInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var score = 0;

        if (input.NameMismatch)
        {
            score += _settings.NameMismatchWeight;
        }

        if (input.DobMismatch)
        {
            score += _settings.DobMismatchWeight;
        }

        if (input.IsPanMissing)
        {
            score += _settings.MissingPanWeight;
        }

        if (input.IsAadhaarMissing)
        {
            score += _settings.MissingAadhaarWeight;
        }

        if (input.FraudDetected)
        {
            score += _settings.FraudDetectedWeight;
        }

        return new RiskAssessmentResult
        {
            Score = score,
            Level = GetRiskLevel(score),
            Recommendation = GetRecommendation(score)
        };
    }

    private static string GetRiskLevel(int score)
    {
        if (score >= 70)
        {
            return "High";
        }

        if (score >= 40)
        {
            return "Medium";
        }

        return "Low";
    }

    private static string GetRecommendation(int score)
    {
        if (score >= 70)
        {
            return "Escalate for manual review and verify supporting documents before proceeding.";
        }

        if (score >= 40)
        {
            return "Conduct additional verification and request missing documents from the candidate.";
        }

        return "Proceed with standard verification and monitor for any new discrepancies.";
    }
}
