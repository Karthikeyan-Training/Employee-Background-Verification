namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Text;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

public class ReportGenerationService : IReportGenerationService
{
    private readonly IOllamaService _ollama;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(IOllamaService ollama, ILogger<ReportGenerationService> logger)
    {
        _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportResult> GenerateAsync(
        BackgroundVerificationRequest request,
        VerificationResult verificationResult,
        FraudResult fraudResult,
        int riskScore,
        string riskLevel)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (verificationResult is null) throw new ArgumentNullException(nameof(verificationResult));
        if (fraudResult is null) throw new ArgumentNullException(nameof(fraudResult));

        var prompt = BuildPrompt(request, verificationResult, fraudResult, riskScore, riskLevel);

        string aiContent;
        try
        {
            _logger.LogInformation("Sending report generation prompt to Ollama for candidate {Candidate}.", request.CandidateName);
            aiContent = await _ollama.GenerateAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unavailable; falling back to structured report for candidate {Candidate}.", request.CandidateName);
            aiContent = BuildFallbackReport(request, verificationResult, fraudResult, riskScore, riskLevel);
        }

        // Ensure the response is non-empty before returning
        if (string.IsNullOrWhiteSpace(aiContent))
        {
            _logger.LogWarning("Ollama returned empty content; using fallback report.");
            aiContent = BuildFallbackReport(request, verificationResult, fraudResult, riskScore, riskLevel);
        }

        return new ReportResult
        {
            ReportId = Guid.NewGuid().ToString("D"),
            Content = aiContent.Trim()
        };
    }

    // -------------------------------------------------------------------------
    // Prompt construction
    // -------------------------------------------------------------------------

    private static string BuildPrompt(
        BackgroundVerificationRequest request,
        VerificationResult verificationResult,
        FraudResult fraudResult,
        int riskScore,
        string riskLevel)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a professional HR compliance analyst. Generate a formal employee background verification report in Markdown.");
        sb.AppendLine("The report MUST contain exactly the following five sections, each with a level-2 heading (##):");
        sb.AppendLine("  1. Executive Summary");
        sb.AppendLine("  2. Verification Findings");
        sb.AppendLine("  3. Fraud Observations");
        sb.AppendLine("  4. Risk Assessment");
        sb.AppendLine("  5. Final Recommendation");
        sb.AppendLine();
        sb.AppendLine("Use clear, concise, professional language. Do not add any other sections or preamble outside the report.");
        sb.AppendLine();

        // Employee details
        sb.AppendLine("## Input Data");
        sb.AppendLine();
        sb.AppendLine("### Employee Details");
        sb.AppendLine($"- **Name:** {request.CandidateName}");
        sb.AppendLine($"- **Email:** {request.Email}");
        sb.AppendLine($"- **Position Applied For:** {request.Position}");
        sb.AppendLine($"- **Criminal Record Check Requested:** {(request.CriminalRecordCheck ? "Yes" : "No")}");
        sb.AppendLine();

        // Verification results
        sb.AppendLine("### Verification Results");
        sb.AppendLine($"- **Overall Status:** {verificationResult.Status}");

        if (verificationResult.MatchedFields.Count > 0)
        {
            sb.AppendLine($"- **Matched Fields:** {string.Join(", ", verificationResult.MatchedFields)}");
        }

        if (verificationResult.MismatchedFields.Count > 0)
        {
            sb.AppendLine("- **Mismatched Fields:**");
            foreach (var mismatch in verificationResult.MismatchedFields)
            {
                var values = string.Join("; ", mismatch.ValuesBySource.Select(kv => $"{kv.Key}=`{kv.Value}`"));
                sb.AppendLine($"  - {mismatch.FieldName} ({values})");
            }
        }

        if (verificationResult.MissingFields.Count > 0)
        {
            sb.AppendLine($"- **Missing Fields:** {string.Join(", ", verificationResult.MissingFields)}");
        }

        if (verificationResult.Warnings.Count > 0)
        {
            sb.AppendLine("- **Warnings:**");
            foreach (var w in verificationResult.Warnings)
                sb.AppendLine($"  - {w}");
        }

        sb.AppendLine();

        // Fraud indicators
        sb.AppendLine("### Fraud Indicators");
        sb.AppendLine($"- **Fraud Detected:** {(fraudResult.IsFraudulent ? "Yes" : "No")}");
        sb.AppendLine($"- **Maximum Severity:** {fraudResult.MaxSeverity}");

        if (fraudResult.Indicators.Count > 0)
        {
            sb.AppendLine("- **Indicators:**");
            foreach (var ind in fraudResult.Indicators)
            {
                sb.AppendLine($"  - [{ind.Severity}] **{ind.IndicatorName}**: {ind.Description}");
            }
        }

        if (fraudResult.ManualReviewRecommendations.Count > 0)
        {
            sb.AppendLine("- **Manual Review Recommendations:**");
            foreach (var rec in fraudResult.ManualReviewRecommendations)
                sb.AppendLine($"  - {rec}");
        }

        sb.AppendLine();

        // Risk score
        sb.AppendLine("### Risk Score");
        sb.AppendLine($"- **Score:** {riskScore} / 100");
        sb.AppendLine($"- **Level:** {riskLevel}");
        sb.AppendLine();

        sb.AppendLine("Now write the five-section Markdown report based on the data above.");

        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Structured fallback (no LLM dependency)
    // -------------------------------------------------------------------------

    private static string BuildFallbackReport(
        BackgroundVerificationRequest request,
        VerificationResult verificationResult,
        FraudResult fraudResult,
        int riskScore,
        string riskLevel)
    {
        var generatedOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        var recommendation = DeriveRecommendation(fraudResult, riskScore);

        var sb = new StringBuilder();

        sb.AppendLine($"# Background Verification Report");
        sb.AppendLine($"**Candidate:** {request.CandidateName}  ");
        sb.AppendLine($"**Position:** {request.Position}  ");
        sb.AppendLine($"**Generated On:** {generatedOn}");
        sb.AppendLine();

        // 1. Executive Summary
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine($"This report presents the findings of a background verification conducted for **{request.CandidateName}** " +
                      $"applying for the role of **{request.Position}**. " +
                      $"The verification returned an overall status of **{verificationResult.Status}** " +
                      $"with a risk score of **{riskScore}/100** ({riskLevel} risk).");
        if (fraudResult.IsFraudulent)
            sb.AppendLine($"Fraud indicators of **{fraudResult.MaxSeverity}** severity were detected and require immediate attention.");
        else
            sb.AppendLine("No fraud indicators were detected during this assessment.");
        sb.AppendLine();

        // 2. Verification Findings
        sb.AppendLine("## Verification Findings");
        sb.AppendLine();
        sb.AppendLine($"| Attribute | Detail |");
        sb.AppendLine($"|-----------|--------|");
        sb.AppendLine($"| Verification Status | {verificationResult.Status} |");
        sb.AppendLine($"| Matched Fields | {(verificationResult.MatchedFields.Count > 0 ? string.Join(", ", verificationResult.MatchedFields) : "None")} |");
        sb.AppendLine($"| Mismatched Fields | {(verificationResult.MismatchedFields.Count > 0 ? string.Join(", ", verificationResult.MismatchedFields.Select(m => m.FieldName)) : "None")} |");
        sb.AppendLine($"| Missing Fields | {(verificationResult.MissingFields.Count > 0 ? string.Join(", ", verificationResult.MissingFields) : "None")} |");
        sb.AppendLine();

        if (verificationResult.MismatchedFields.Count > 0)
        {
            sb.AppendLine("### Mismatch Details");
            sb.AppendLine();
            foreach (var mismatch in verificationResult.MismatchedFields)
            {
                sb.AppendLine($"**{mismatch.FieldName}**");
                foreach (var kv in mismatch.ValuesBySource)
                    sb.AppendLine($"- {kv.Key}: `{kv.Value}`");
                sb.AppendLine();
            }
        }

        if (verificationResult.Warnings.Count > 0)
        {
            sb.AppendLine("### Warnings");
            sb.AppendLine();
            foreach (var w in verificationResult.Warnings)
                sb.AppendLine($"- {w}");
            sb.AppendLine();
        }

        // 3. Fraud Observations
        sb.AppendLine("## Fraud Observations");
        sb.AppendLine();
        if (!fraudResult.IsFraudulent)
        {
            sb.AppendLine("No fraud indicators were identified during document and data analysis.");
        }
        else
        {
            sb.AppendLine($"> **Alert:** Fraud detected at **{fraudResult.MaxSeverity}** severity.");
            sb.AppendLine();
            sb.AppendLine("| Indicator | Severity | Description |");
            sb.AppendLine("|-----------|----------|-------------|");
            foreach (var ind in fraudResult.Indicators)
                sb.AppendLine($"| {ind.IndicatorName} | {ind.Severity} | {ind.Description} |");

            if (fraudResult.ManualReviewRecommendations.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Manual Review Recommendations");
                sb.AppendLine();
                foreach (var rec in fraudResult.ManualReviewRecommendations)
                    sb.AppendLine($"- {rec}");
            }
        }
        sb.AppendLine();

        // 4. Risk Assessment
        sb.AppendLine("## Risk Assessment");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Value |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Risk Score | {riskScore} / 100 |");
        sb.AppendLine($"| Risk Level | {riskLevel} |");
        sb.AppendLine($"| Fraud Detected | {(fraudResult.IsFraudulent ? "Yes" : "No")} |");
        sb.AppendLine($"| Criminal Check Requested | {(request.CriminalRecordCheck ? "Yes" : "No")} |");
        sb.AppendLine();
        sb.AppendLine(DescribeRiskLevel(riskScore, riskLevel));
        sb.AppendLine();

        // 5. Final Recommendation
        sb.AppendLine("## Final Recommendation");
        sb.AppendLine();
        sb.AppendLine(recommendation);
        sb.AppendLine();

        return sb.ToString();
    }

    private static string DescribeRiskLevel(int riskScore, string riskLevel) => riskLevel.ToUpperInvariant() switch
    {
        "LOW" or "NONE" => $"The candidate presents a **low risk** (score: {riskScore}). Background data is consistent and no significant concerns were identified.",
        "MEDIUM" or "MODERATE" => $"The candidate presents a **moderate risk** (score: {riskScore}). Some discrepancies were noted and should be clarified before proceeding.",
        "HIGH" => $"The candidate presents a **high risk** (score: {riskScore}). Multiple concerns have been identified that may disqualify the candidate.",
        "CRITICAL" => $"The candidate presents a **critical risk** (score: {riskScore}). Serious fraud or data integrity issues were found. Immediate escalation is required.",
        _ => $"Risk level is **{riskLevel}** with a score of {riskScore}."
    };

    private static string DeriveRecommendation(FraudResult fraudResult, int riskScore)
    {
        if (fraudResult.IsFraudulent && fraudResult.MaxSeverity >= FraudSeverity.High)
            return "**Do Not Hire.** Critical or high-severity fraud indicators have been detected. This application must be escalated to the compliance team and legal department before any further action is taken.";

        if (riskScore >= 70)
            return "**Conditional — Further Review Required.** The candidate's risk score is elevated. HR should conduct an in-person interview to address the identified discrepancies before a hiring decision is made.";

        if (riskScore >= 40)
            return "**Proceed with Caution.** Minor discrepancies exist. The hiring manager should review the flagged fields and obtain clarification directly from the candidate.";

        return "**Cleared to Hire.** The background verification found no material issues. The candidate may proceed to the next stage of the recruitment process.";
    }
}
