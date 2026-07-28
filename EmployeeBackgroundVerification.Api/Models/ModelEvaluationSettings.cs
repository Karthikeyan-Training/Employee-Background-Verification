namespace EmployeeBackgroundVerification.Api.Models;

using System.Collections.Generic;

/// <summary>Configuration for a single model to be evaluated.</summary>
public sealed class EvaluationModelEntry
{
    /// <summary>Human-readable name shown in reports (e.g. "Llama 3.2").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Ollama model identifier used in API calls (e.g. "llama3.2", "phi3:mini").</summary>
    public string ModelId { get; set; } = string.Empty;
}

/// <summary>Top-level settings for the ModelEvaluationService.</summary>
public sealed class ModelEvaluationSettings
{
    /// <summary>Ollama base URL. Defaults to the shared Ollama instance.</summary>
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Per-request HTTP timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Models to include in the evaluation.</summary>
    public List<EvaluationModelEntry> Models { get; set; } = new()
    {
        new EvaluationModelEntry { DisplayName = "Llama 3.2",  ModelId = "llama3.2"  },
        new EvaluationModelEntry { DisplayName = "Phi-3 Mini", ModelId = "phi3:mini" }
    };

    /// <summary>
    /// Weights used to compute the composite score (values are relative, not required to sum to 100).
    /// </summary>
    public CompositeWeights Weights { get; set; } = new();
}

/// <summary>Relative weights for the composite scoring formula.</summary>
public sealed class CompositeWeights
{
    public double Accuracy      { get; set; } = 35;
    public double Latency       { get; set; } = 20;
    public double InferenceTime { get; set; } = 15;
    public double Memory        { get; set; } = 10;
    public double OutputQuality { get; set; } = 20;
}
