using EmployeeBackgroundVerification.Api.Models;

namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

public interface IRiskScoringService
{
    RiskAssessmentResult Assess(RiskScoringInput input);
}
