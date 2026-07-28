namespace EmployeeBackgroundVerification.Api.Models;

using System;
using System.Collections.Generic;

/// <summary>The three prompting strategies under evaluation.</summary>
public enum PromptStrategy
{
    ZeroShot,
    FewShot,
    ChainOfThought
}

/// <summary>Result for a single prompting strategy run against an employee document.</summary>
public sealed record PromptExperimentResult
{
    /// <summary>Strategy used in this experiment run.</summary>
    public PromptStrategy Strategy { get; init; }

    /// <summary>Human-readable strategy label.</summary>
    public string StrategyLabel { get; init; } = string.Empty;

    /// <summary>The exact prompt that was sent to the model.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Raw text output returned by the model.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>Wall-clock execution time from request dispatch to full response (ms).</summary>
    public double ExecutionTimeMs { get; init; }

    /// <summary>
    /// Observed accuracy score (0–100): percentage of expected document fields
    /// correctly identified in the model output.
    /// </summary>
    public double ObservedAccuracy { get; init; }

    /// <summary>Whether the model responded successfully.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Error message if the call failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Individual field-level accuracy breakdown.</summary>
    public IReadOnlyList<FieldAccuracyDetail> FieldAccuracyDetails { get; init; }
        = Array.Empty<FieldAccuracyDetail>();
}

/// <summary>Per-field accuracy detail for a single experiment result.</summary>
public sealed record FieldAccuracyDetail
{
    public string FieldName { get; init; } = string.Empty;
    public string ExpectedValue { get; init; } = string.Empty;
    public bool FoundInOutput { get; init; }
}

/// <summary>Full comparison report across all three prompting strategies.</summary>
public sealed record PromptExperimentReport
{
    /// <summary>Unique identifier for this report.</summary>
    public string ReportId { get; init; } = string.Empty;

    /// <summary>UTC timestamp of when the experiment ran.</summary>
    public DateTime GeneratedOn { get; init; }

    /// <summary>Name of the Ollama model used for all three runs.</summary>
    public string ModelUsed { get; init; } = string.Empty;

    /// <summary>Results ordered by strategy (ZeroShot → FewShot → ChainOfThought).</summary>
    public IReadOnlyList<PromptExperimentResult> Results { get; init; }
        = Array.Empty<PromptExperimentResult>();

    /// <summary>Strategy with the highest observed accuracy.</summary>
    public string BestStrategy { get; init; } = string.Empty;

    /// <summary>Markdown comparison table.</summary>
    public string ComparisonTable { get; init; } = string.Empty;

    /// <summary>Full Markdown comparison report.</summary>
    public string FullReport { get; init; } = string.Empty;
}
