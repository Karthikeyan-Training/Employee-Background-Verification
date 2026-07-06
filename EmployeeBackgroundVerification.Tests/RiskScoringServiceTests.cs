using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace EmployeeBackgroundVerification.Tests;

public sealed class RiskScoringServiceTests
{
    [Fact]
    public void Assess_ReturnsExpectedScoreAndLevel_ForMultipleRisks()
    {
        var service = new RiskScoringService(Options.Create(new RiskScoringSettings()));

        var result = service.Assess(new RiskScoringInput
        {
            NameMismatch = true,
            DobMismatch = true,
            IsPanMissing = true,
            IsAadhaarMissing = true,
            FraudDetected = true
        });

        Assert.Equal(140, result.Score);
        Assert.Equal("High", result.Level);
        Assert.Equal("Escalate for manual review and verify supporting documents before proceeding.", result.Recommendation);
    }

    [Fact]
    public void Assess_ReturnsLowRisk_WhenNoRulesTriggered()
    {
        var service = new RiskScoringService(Options.Create(new RiskScoringSettings()));

        var result = service.Assess(new RiskScoringInput());

        Assert.Equal(0, result.Score);
        Assert.Equal("Low", result.Level);
        Assert.Equal("Proceed with standard verification and monitor for any new discrepancies.", result.Recommendation);
    }
}
