namespace EmployeeBackgroundVerification.Api.DTOs;

using System;
using System.Collections.Generic;

// ── Per-pipeline-step timing ──────────────────────────────────────────────────

/// <summary>Timing and status for a single pipeline stage.</summary>
public sealed class PipelineStepDto
{
    public string Step { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;   // Completed | Skipped | Failed
    public double DurationMs { get; init; }
    public string? Detail { get; init; }
}

// ── Per-document results ──────────────────────────────────────────────────────

/// <summary>OCR and extraction result for a single uploaded file.</summary>
public sealed class DocumentProcessingResultDto
{
    public string FileName { get; init; } = string.Empty;
    public string OcrText { get; init; } = string.Empty;
    public double OcrDurationMs { get; init; }
    public ExtractedDocumentDto ExtractedFields { get; init; } = new();
    public double ExtractionDurationMs { get; init; }
}

/// <summary>Structured fields extracted from a document via LLM.</summary>
public sealed class ExtractedDocumentDto
{
    public string FullName { get; init; } = string.Empty;
    public string DateOfBirth { get; init; } = string.Empty;
    public string AadhaarNumber { get; init; } = string.Empty;
    public string PanNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Degree { get; init; } = string.Empty;
    public string University { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
}

// ── Verification summary ──────────────────────────────────────────────────────

public sealed class VerificationSummaryDto
{
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<string> MatchedFields { get; init; } = Array.Empty<string>();
    public IReadOnlyList<MismatchedFieldDto> MismatchedFields { get; init; } = Array.Empty<MismatchedFieldDto>();
    public IReadOnlyList<string> MissingFields { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class MismatchedFieldDto
{
    public string FieldName { get; init; } = string.Empty;
    public Dictionary<string, string> ValuesBySource { get; init; } = new();
}

// ── Fraud summary ─────────────────────────────────────────────────────────────

public sealed class FraudSummaryDto
{
    public bool IsFraudulent { get; init; }
    public string MaxSeverity { get; init; } = string.Empty;
    public IReadOnlyList<FraudIndicatorDto> Indicators { get; init; } = Array.Empty<FraudIndicatorDto>();
    public IReadOnlyList<string> ManualReviewRecommendations { get; init; } = Array.Empty<string>();
}

public sealed class FraudIndicatorDto
{
    public string IndicatorName { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

// ── Risk summary ──────────────────────────────────────────────────────────────

public sealed class RiskSummaryDto
{
    public int Score { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

// ── Top-level demo response ───────────────────────────────────────────────────

/// <summary>Complete result returned by POST /api/demo/run.</summary>
public sealed class DemoRunResponseDto
{
    // Candidate
    public string CandidateName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;

    // Pipeline execution log
    public IReadOnlyList<PipelineStepDto> PipelineSteps { get; init; } = Array.Empty<PipelineStepDto>();

    // Per-document detail
    public IReadOnlyList<DocumentProcessingResultDto> Documents { get; init; } = Array.Empty<DocumentProcessingResultDto>();

    // Stage results
    public VerificationSummaryDto Verification { get; init; } = new();
    public FraudSummaryDto Fraud { get; init; } = new();
    public RiskSummaryDto Risk { get; init; } = new();

    // Report
    public string ReportId { get; init; } = string.Empty;
    public string ReportContent { get; init; } = string.Empty;

    // Meta
    public DateTime CompletedOn { get; init; }
    public double TotalDurationMs { get; init; }
}
