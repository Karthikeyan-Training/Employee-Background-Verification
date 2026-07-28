namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ModelEvaluationService : IModelEvaluationService
{
    // ── Domain-specific test prompts with expected keyword sets ───────────────
    private static readonly IReadOnlyList<TestPrompt> TestPrompts = new[]
    {
        new TestPrompt(
            "What documents are typically required for an employee background verification?",
            new[] { "aadhaar", "pan", "passport", "resume", "identity", "address", "proof", "document" }),

        new TestPrompt(
            "List three common fraud indicators found during background checks.",
            new[] { "fraud", "fake", "forged", "mismatch", "discrepancy", "duplicate", "tamper", "false" }),

        new TestPrompt(
            "Explain what a risk score means in the context of employee background verification.",
            new[] { "risk", "score", "threshold", "level", "assessment", "hire", "concern", "indicator" }),

        new TestPrompt(
            "Summarise the key steps in an employee background verification process in 3 sentences.",
            new[] { "collect", "verify", "report", "document", "check", "identity", "result", "finding" }),

        new TestPrompt(
            "What actions should HR take when a background check returns a HIGH risk score?",
            new[] { "escalate", "review", "interview", "legal", "compliance", "decision", "action", "caution" })
    };

    // ── Ollama response shape (non-streaming) ─────────────────────────────────
    private sealed record OllamaGenerateResponse(
        string Model,
        string Response,
        bool Done,
        long TotalDuration,
        long LoadDuration,
        int PromptEvalCount,
        long PromptEvalDuration,
        int EvalCount,
        long EvalDuration);

    // ── Ollama /api/ps model entry ────────────────────────────────────────────
    private sealed record OllamaPsModel(string Name, long Size);
    private sealed record OllamaPsResponse(IReadOnlyList<OllamaPsModel> Models);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ModelEvaluationSettings _settings;
    private readonly ILogger<ModelEvaluationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public ModelEvaluationService(
        IHttpClientFactory httpClientFactory,
        IOptions<ModelEvaluationSettings> settings,
        ILogger<ModelEvaluationService> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public async Task<ModelComparisonReport> EvaluateModelsAsync()
    {
        if (_settings.Models.Count == 0)
            throw new InvalidOperationException("No models configured for evaluation.");

        _logger.LogInformation("Starting model evaluation for {Count} model(s).", _settings.Models.Count);

        using var httpClient = CreateHttpClient();

        // Evaluate all models
        var rawResults = new List<ModelEvaluationResult>();
        foreach (var entry in _settings.Models)
        {
            var result = await EvaluateSingleModelAsync(httpClient, entry);
            rawResults.Add(result);
        }

        // Rank and compute composite scores
        var ranked = RankResults(rawResults);

        // Build report artefacts
        var table        = BuildComparisonTable(ranked);
        var recommendation = BuildRecommendation(ranked);
        var fullReport   = BuildFullReport(ranked, table, recommendation);

        var report = new ModelComparisonReport
        {
            ReportId        = Guid.NewGuid().ToString("D"),
            GeneratedOn     = DateTime.UtcNow,
            ModelResults    = ranked,
            ComparisonTable = table,
            RecommendedModel = ranked.FirstOrDefault(r => r.Rank == 1)?.DisplayName ?? "N/A",
            Recommendation  = recommendation,
            FullReport      = fullReport
        };

        _logger.LogInformation("Model evaluation complete. Recommended: {Model}", report.RecommendedModel);
        return report;
    }

    // =========================================================================
    // Single-model evaluation
    // =========================================================================

    private async Task<ModelEvaluationResult> EvaluateSingleModelAsync(
        HttpClient httpClient,
        EvaluationModelEntry entry)
    {
        _logger.LogInformation("Evaluating model '{Model}'.", entry.DisplayName);

        var promptResults = new List<PromptEvaluationResult>();

        foreach (var testPrompt in TestPrompts)
        {
            var result = await RunPromptAsync(httpClient, entry.ModelId, testPrompt);
            promptResults.Add(result);
        }

        var succeeded = promptResults.Where(r => r.Succeeded).ToList();
        bool isAvailable = succeeded.Count > 0;

        if (!isAvailable)
        {
            _logger.LogWarning("Model '{Model}' produced no successful responses.", entry.DisplayName);
            return new ModelEvaluationResult
            {
                DisplayName       = entry.DisplayName,
                ModelId           = entry.ModelId,
                IsAvailable       = false,
                UnavailableReason = promptResults.FirstOrDefault()?.ErrorMessage ?? "Model did not respond.",
                PromptResults     = promptResults
            };
        }

        // Aggregate metrics
        double avgLatency       = succeeded.Average(r => r.LatencyMs);
        double minLatency       = succeeded.Min(r => r.LatencyMs);
        double maxLatency       = succeeded.Max(r => r.LatencyMs);
        double avgInference     = succeeded.Average(r => r.InferenceTimeMs);
        double avgTps           = succeeded.Average(r => r.TokensPerSecond);
        double avgAccuracy      = succeeded.Average(r => r.AccuracyScore);
        double avgQuality       = succeeded.Average(r => r.QualityScore);

        // Memory: query /api/ps
        (long memBytes, string memFormatted) = await GetModelMemoryAsync(httpClient, entry.ModelId);

        return new ModelEvaluationResult
        {
            DisplayName              = entry.DisplayName,
            ModelId                  = entry.ModelId,
            IsAvailable              = true,
            AccuracyScore            = Math.Round(avgAccuracy, 2),
            AverageLatencyMs         = Math.Round(avgLatency, 2),
            MinLatencyMs             = Math.Round(minLatency, 2),
            MaxLatencyMs             = Math.Round(maxLatency, 2),
            MemoryUsageBytes         = memBytes,
            MemoryUsageFormatted     = memFormatted,
            AverageInferenceTimeMs   = Math.Round(avgInference, 2),
            AverageTokensPerSecond   = Math.Round(avgTps, 2),
            OutputQualityScore       = Math.Round(avgQuality, 2),
            PromptResults            = promptResults
        };
    }

    // =========================================================================
    // Prompt execution
    // =========================================================================

    private async Task<PromptEvaluationResult> RunPromptAsync(
        HttpClient httpClient,
        string modelId,
        TestPrompt testPrompt)
    {
        var payload = new
        {
            model  = modelId,
            prompt = testPrompt.Text,
            stream = false
        };

        var json    = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await httpClient.PostAsync("/api/generate", content);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return FailedResult(testPrompt.Text, $"HTTP {(int)response.StatusCode}: {err}", sw.Elapsed.TotalMilliseconds);
            }

            var body = await response.Content.ReadAsStringAsync();
            OllamaGenerateResponse? parsed;

            try
            {
                parsed = JsonSerializer.Deserialize<OllamaGenerateResponse>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                return FailedResult(testPrompt.Text, $"JSON parse error: {ex.Message}", sw.Elapsed.TotalMilliseconds);
            }

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Response))
                return FailedResult(testPrompt.Text, "Empty response from model.", sw.Elapsed.TotalMilliseconds);

            double inferenceMs   = parsed.EvalDuration > 0 ? parsed.EvalDuration / 1_000_000.0 : sw.Elapsed.TotalMilliseconds;
            double tokensPerSec  = (inferenceMs > 0 && parsed.EvalCount > 0)
                                        ? parsed.EvalCount / (inferenceMs / 1000.0)
                                        : 0;

            double accuracyScore = ScoreAccuracy(parsed.Response, testPrompt.ExpectedKeywords);
            double qualityScore  = ScoreQuality(parsed.Response);

            return new PromptEvaluationResult
            {
                Prompt          = testPrompt.Text,
                Response        = parsed.Response,
                LatencyMs       = sw.Elapsed.TotalMilliseconds,
                InferenceTimeMs = Math.Round(inferenceMs, 2),
                TokensPerSecond = Math.Round(tokensPerSec, 2),
                AccuracyScore   = Math.Round(accuracyScore, 2),
                QualityScore    = Math.Round(qualityScore, 2),
                Succeeded       = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Prompt failed for model (latency {Latency}ms).", sw.Elapsed.TotalMilliseconds);
            return FailedResult(testPrompt.Text, ex.Message, sw.Elapsed.TotalMilliseconds);
        }
    }

    // =========================================================================
    // Memory query
    // =========================================================================

    private async Task<(long Bytes, string Formatted)> GetModelMemoryAsync(HttpClient httpClient, string modelId)
    {
        try
        {
            using var response = await httpClient.GetAsync("/api/ps");
            if (!response.IsSuccessStatusCode)
                return (0, "N/A");

            var body = await response.Content.ReadAsStringAsync();
            var ps   = JsonSerializer.Deserialize<OllamaPsResponse>(body, JsonOptions);
            var entry = ps?.Models?.FirstOrDefault(m =>
                m.Name.StartsWith(modelId, StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                return (0, "Not loaded");

            return (entry.Size, FormatBytes(entry.Size));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve memory info for model '{Model}'.", modelId);
            return (0, "N/A");
        }
    }

    // =========================================================================
    // Scoring helpers
    // =========================================================================

    /// <summary>
    /// Keyword-match accuracy: percentage of expected keywords found (case-insensitive)
    /// in the model's response, scaled to 0–100.
    /// </summary>
    private static double ScoreAccuracy(string response, IReadOnlyList<string> expectedKeywords)
    {
        if (expectedKeywords.Count == 0) return 100;
        var lower = response.ToLowerInvariant();
        int hits  = expectedKeywords.Count(kw => lower.Contains(kw));
        return hits / (double)expectedKeywords.Count * 100.0;
    }

    /// <summary>
    /// Heuristic output-quality score (0–100) based on:
    ///   • Response length   (30 pts)  — penalises very short or excessively long answers
    ///   • Structure markers (30 pts)  — bullets, numbered lists, headings
    ///   • Sentence count    (20 pts)  — rewards multi-sentence answers
    ///   • Vocabulary richness (20 pts) — unique word ratio
    /// </summary>
    private static double ScoreQuality(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return 0;

        // Length score: ideal band 150–800 chars
        int len         = response.Length;
        double lenScore = len < 50  ? 5
                        : len < 150 ? 15
                        : len < 800 ? 30
                        : len < 1500 ? 20
                        : 10;

        // Structure score
        int bullets   = response.Split('\n').Count(l => l.TrimStart().StartsWith('-') || l.TrimStart().StartsWith('*') || l.TrimStart().StartsWith("•"));
        int numbered  = response.Split('\n').Count(l => l.TrimStart().Length > 0 && char.IsDigit(l.TrimStart()[0]) && l.TrimStart().Contains('.'));
        int headings  = response.Split('\n').Count(l => l.TrimStart().StartsWith('#'));
        double structScore = Math.Min(30, (bullets + numbered) * 5 + headings * 3);

        // Sentence count score
        int sentences   = response.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        double sentScore = sentences switch { >= 4 => 20, >= 2 => 14, >= 1 => 8, _ => 0 };

        // Vocabulary richness
        var words        = response.ToLowerInvariant().Split(new[] { ' ', '\n', '\r', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        double richScore = words.Length > 0
            ? Math.Min(20, (double)words.Distinct().Count() / words.Length * 40)
            : 0;

        return Math.Round(lenScore + structScore + sentScore + richScore, 2);
    }

    // =========================================================================
    // Ranking
    // =========================================================================

    private IReadOnlyList<ModelEvaluationResult> RankResults(List<ModelEvaluationResult> results)
    {
        // Unavailable models get rank last; score 0
        var available   = results.Where(r => r.IsAvailable).ToList();
        var unavailable = results.Where(r => !r.IsAvailable).ToList();

        if (available.Count == 0)
        {
            return results.Select((r, i) => r with { Rank = i + 1, CompositeScore = 0 }).ToList();
        }

        var w = _settings.Weights;
        double totalWeight = w.Accuracy + w.Latency + w.InferenceTime + w.Memory + w.OutputQuality;

        // Normalise latency and inference — lower is better, so we invert
        double maxLatency   = available.Max(r => r.AverageLatencyMs);
        double maxInference = available.Max(r => r.AverageInferenceTimeMs);
        double maxMemory    = available.Max(r => r.MemoryUsageBytes > 0 ? (double)r.MemoryUsageBytes : 1);

        var scored = available.Select(r =>
        {
            double latencyScore   = maxLatency   > 0 ? (1 - r.AverageLatencyMs         / maxLatency)   * 100 : 100;
            double inferenceScore = maxInference > 0 ? (1 - r.AverageInferenceTimeMs   / maxInference) * 100 : 100;
            double memScore       = maxMemory    > 0 && r.MemoryUsageBytes > 0
                                        ? (1 - r.MemoryUsageBytes / maxMemory) * 100
                                        : 50; // neutral if unknown

            double composite = (
                r.AccuracyScore      * w.Accuracy      +
                latencyScore         * w.Latency        +
                inferenceScore       * w.InferenceTime  +
                memScore             * w.Memory         +
                r.OutputQualityScore * w.OutputQuality
            ) / totalWeight;

            return r with { CompositeScore = Math.Round(composite, 2) };
        })
        .OrderByDescending(r => r.CompositeScore)
        .Select((r, i) => r with { Rank = i + 1 })
        .ToList();

        // Append unavailable models after the ranked ones
        int nextRank = scored.Count + 1;
        var unavailableRanked = unavailable
            .Select(r => r with { Rank = nextRank++, CompositeScore = 0 })
            .ToList();

        return scored.Concat(unavailableRanked).ToList();
    }

    // =========================================================================
    // Report builders
    // =========================================================================

    private static string BuildComparisonTable(IReadOnlyList<ModelEvaluationResult> ranked)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Model Comparison Table");
        sb.AppendLine();
        sb.AppendLine("| Rank | Model | Accuracy (0–100) | Avg Latency (ms) | Avg Inference (ms) | Tokens/s | Memory | Quality (0–100) | Composite Score | Available |");
        sb.AppendLine("|------|-------|-----------------|------------------|--------------------|----------|--------|----------------|----------------|-----------|");

        foreach (var r in ranked)
        {
            string available = r.IsAvailable ? "✅" : "❌";
            string accuracy  = r.IsAvailable ? r.AccuracyScore.ToString("F1")            : "—";
            string latency   = r.IsAvailable ? r.AverageLatencyMs.ToString("F0")         : "—";
            string inference = r.IsAvailable ? r.AverageInferenceTimeMs.ToString("F0")   : "—";
            string tps       = r.IsAvailable ? r.AverageTokensPerSecond.ToString("F1")   : "—";
            string memory    = r.MemoryUsageFormatted;
            string quality   = r.IsAvailable ? r.OutputQualityScore.ToString("F1")       : "—";
            string composite = r.IsAvailable ? r.CompositeScore.ToString("F1")           : "0.0";

            sb.AppendLine($"| {r.Rank} | **{r.DisplayName}** | {accuracy} | {latency} | {inference} | {tps} | {memory} | {quality} | {composite} | {available} |");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildRecommendation(IReadOnlyList<ModelEvaluationResult> ranked)
    {
        var best = ranked.FirstOrDefault(r => r.Rank == 1 && r.IsAvailable);

        if (best is null)
        {
            return "## Final Recommendation\n\nNo models were available during evaluation. " +
                   "Ensure Ollama is running and the configured models are pulled locally.\n";
        }

        var others = ranked.Where(r => r.IsAvailable && r.Rank > 1).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("## Final Recommendation");
        sb.AppendLine();
        sb.AppendLine($"### Recommended Model: **{best.DisplayName}** (`{best.ModelId}`)");
        sb.AppendLine();
        sb.AppendLine("#### Justification");
        sb.AppendLine();
        sb.AppendLine($"**{best.DisplayName}** achieved the highest composite score of **{best.CompositeScore:F1}/100** " +
                      "based on a weighted evaluation of accuracy, latency, inference time, memory usage, and output quality.");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Accuracy Score | {best.AccuracyScore:F1} / 100 |");
        sb.AppendLine($"| Average Latency | {best.AverageLatencyMs:F0} ms |");
        sb.AppendLine($"| Average Inference Time | {best.AverageInferenceTimeMs:F0} ms |");
        sb.AppendLine($"| Tokens per Second | {best.AverageTokensPerSecond:F1} |");
        sb.AppendLine($"| Memory Usage | {best.MemoryUsageFormatted} |");
        sb.AppendLine($"| Output Quality Score | {best.OutputQualityScore:F1} / 100 |");
        sb.AppendLine($"| Composite Score | {best.CompositeScore:F1} / 100 |");
        sb.AppendLine();

        if (others.Count > 0)
        {
            sb.AppendLine("#### Comparison with Other Models");
            sb.AppendLine();
            foreach (var other in others)
            {
                double scoreDiff = best.CompositeScore - other.CompositeScore;
                sb.AppendLine($"- **{other.DisplayName}** scored {other.CompositeScore:F1}/100 " +
                              $"({scoreDiff:+F1;-F1} relative to the recommended model). " +
                              BuildTradeOffNote(best, other));
            }
            sb.AppendLine();
        }

        sb.AppendLine("#### Suitability for Employee Background Verification");
        sb.AppendLine();
        sb.AppendLine($"Given the domain requirements — accurate document analysis, low-latency fraud detection responses, " +
                      $"and professional report generation — **{best.DisplayName}** provides the best balance of response " +
                      $"quality and inference speed for production use in this system.");

        return sb.ToString();
    }

    private static string BuildTradeOffNote(ModelEvaluationResult best, ModelEvaluationResult other)
    {
        var notes = new List<string>();
        if (other.AverageLatencyMs < best.AverageLatencyMs)
            notes.Add($"lower latency ({other.AverageLatencyMs:F0} ms vs {best.AverageLatencyMs:F0} ms)");
        if (other.MemoryUsageBytes > 0 && other.MemoryUsageBytes < best.MemoryUsageBytes)
            notes.Add($"smaller memory footprint ({other.MemoryUsageFormatted} vs {best.MemoryUsageFormatted})");
        if (other.AccuracyScore > best.AccuracyScore)
            notes.Add($"higher accuracy ({other.AccuracyScore:F1} vs {best.AccuracyScore:F1})");

        return notes.Count > 0
            ? $"It has advantages in: {string.Join(", ", notes)}."
            : "It did not outperform the recommended model on any weighted metric.";
    }

    private static string BuildFullReport(
        IReadOnlyList<ModelEvaluationResult> ranked,
        string table,
        string recommendation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# LLM Model Evaluation Report");
        sb.AppendLine($"**Generated On:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC  ");
        sb.AppendLine($"**Models Evaluated:** {string.Join(", ", ranked.Select(r => r.DisplayName))}");
        sb.AppendLine($"**Test Prompts:** {TestPrompts.Count} domain-specific background verification prompts");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(table);
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Metric Definitions");
        sb.AppendLine();
        sb.AppendLine("| Metric | Description |");
        sb.AppendLine("|--------|-------------|");
        sb.AppendLine("| **Accuracy** | Percentage of expected domain keywords present in model responses (0–100). |");
        sb.AppendLine("| **Avg Latency** | Wall-clock time from HTTP request dispatch to full response received (ms). |");
        sb.AppendLine("| **Avg Inference Time** | Token-generation time (`eval_duration`) reported by Ollama (ms). |");
        sb.AppendLine("| **Tokens/s** | Tokens generated per second during inference. |");
        sb.AppendLine("| **Memory** | Model size in RAM/VRAM as reported by Ollama `/api/ps`. |");
        sb.AppendLine("| **Quality** | Heuristic score (0–100) based on response length, structure, and vocabulary richness. |");
        sb.AppendLine("| **Composite Score** | Weighted aggregate of all metrics (Accuracy 35%, Quality 20%, Latency 20%, Inference 15%, Memory 10%). |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(recommendation);

        return sb.ToString();
    }

    // =========================================================================
    // Utilities
    // =========================================================================

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("ModelEvaluation");
        client.BaseAddress = new Uri(_settings.OllamaBaseUrl);
        client.Timeout     = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        return client;
    }

    private static PromptEvaluationResult FailedResult(string prompt, string error, double latencyMs) =>
        new()
        {
            Prompt        = prompt,
            Response      = string.Empty,
            LatencyMs     = latencyMs,
            Succeeded     = false,
            ErrorMessage  = error
        };

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "N/A";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:F1} {units[unit]}";
    }

    // =========================================================================
    // Inner types
    // =========================================================================

    private sealed record TestPrompt(string Text, IReadOnlyList<string> ExpectedKeywords);
}
