namespace EmployeeBackgroundVerification.Api.Models;

using System;
using System.Collections.Generic;

/// <summary>Metrics captured for a single prompt run against one model.</summary>
public sealed record PromptEvaluationResult
{
    /// <summary>The prompt that was sent to the model.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Raw text returned by the model.</summary>
    public string Response { get; init; } = string.Empty;

    /// <summary>Wall-clock time from request dispatch to full response (ms).</summary>
    public double LatencyMs { get; init; }

    /// <summary>Token-generation time reported by Ollama (eval_duration → ms).</summary>
    public double InferenceTimeMs { get; init; }

    /// <summary>Tokens generated per second (eval_count / eval_duration).</summary>
    public double TokensPerSecond { get; init; }

    /// <summary>Keyword-match accuracy score for this prompt (0–100).</summary>
    public double AccuracyScore { get; init; }

    /// <summary>Heuristic output-quality score for this prompt (0–100).</summary>
    public double QualityScore { get; init; }

    /// <summary>Whether the model returned a usable response.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Error message if the call failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>Aggregated evaluation metrics for a single model.</summary>
public sealed record ModelEvaluationResult
{
    /// <summary>Human-readable model display name (e.g. "Llama 3.2").</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Ollama model identifier (e.g. "llama3.2", "phi3:mini").</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Whether the model was reachable during evaluation.</summary>
    public bool IsAvailable { get; init; }

    /// <summary>Reason the model was unavailable, if applicable.</summary>
    public string? UnavailableReason { get; init; }

    // ── Accuracy ──────────────────────────────────────────────────────────────

    /// <summary>Average keyword-match accuracy across all prompts (0–100).</summary>
    public double AccuracyScore { get; init; }

    // ── Latency ───────────────────────────────────────────────────────────────

    /// <summary>Average wall-clock latency per request (ms).</summary>
    public double AverageLatencyMs { get; init; }

    /// <summary>Minimum observed latency (ms).</summary>
    public double MinLatencyMs { get; init; }

    /// <summary>Maximum observed latency (ms).</summary>
    public double MaxLatencyMs { get; init; }

    // ── Memory ────────────────────────────────────────────────────────────────

    /// <summary>Model size in bytes as reported by Ollama /api/ps (0 if unavailable).</summary>
    public long MemoryUsageBytes { get; init; }

    /// <summary>Human-readable memory size (e.g. "3.8 GB").</summary>
    public string MemoryUsageFormatted { get; init; } = "N/A";

    // ── Inference Time ────────────────────────────────────────────────────────

    /// <summary>Average Ollama eval_duration per request (ms).</summary>
    public double AverageInferenceTimeMs { get; init; }

    /// <summary>Average tokens generated per second.</summary>
    public double AverageTokensPerSecond { get; init; }

    // ── Output Quality ────────────────────────────────────────────────────────

    /// <summary>Heuristic output-quality score (0–100).</summary>
    public double OutputQualityScore { get; init; }

    // ── Composite ─────────────────────────────────────────────────────────────

    /// <summary>Weighted composite score used for ranking (0–100).</summary>
    public double CompositeScore { get; init; }

    /// <summary>Rank among evaluated models (1 = best).</summary>
    public int Rank { get; init; }

    // ── Detail ────────────────────────────────────────────────────────────────

    /// <summary>Per-prompt breakdown.</summary>
    public IReadOnlyList<PromptEvaluationResult> PromptResults { get; init; }
        = Array.Empty<PromptEvaluationResult>();
}

/// <summary>Full comparison report across all evaluated models.</summary>
public sealed record ModelComparisonReport
{
    /// <summary>Unique identifier for this report.</summary>
    public string ReportId { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the evaluation was run.</summary>
    public DateTime GeneratedOn { get; init; }

    /// <summary>Per-model evaluation results, ordered by rank.</summary>
    public IReadOnlyList<ModelEvaluationResult> ModelResults { get; init; }
        = Array.Empty<ModelEvaluationResult>();

    /// <summary>Markdown-formatted comparison table.</summary>
    public string ComparisonTable { get; init; } = string.Empty;

    /// <summary>Display name of the recommended model.</summary>
    public string RecommendedModel { get; init; } = string.Empty;

    /// <summary>Full Markdown recommendation with justification.</summary>
    public string Recommendation { get; init; } = string.Empty;

    /// <summary>Complete Markdown report combining table and recommendation.</summary>
    public string FullReport { get; init; } = string.Empty;
}
