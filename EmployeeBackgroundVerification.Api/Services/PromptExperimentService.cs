namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class PromptExperimentService : IPromptExperimentService
{
    private readonly IOllamaService _ollama;
    private readonly OllamaSettings _ollamaSettings;
    private readonly ILogger<PromptExperimentService> _logger;

    // Fields evaluated for accuracy (field name → accessor)
    private static readonly IReadOnlyList<(string Label, Func<DocumentDetails, string> Get)> TrackedFields =
        new (string, Func<DocumentDetails, string>)[]
        {
            ("Full Name",    d => d.FullName),
            ("Date of Birth", d => d.DateOfBirth),
            ("Aadhaar Number", d => d.AadhaarNumber),
            ("PAN Number",   d => d.PanNumber),
            ("Address",      d => d.Address),
            ("Degree",       d => d.Degree),
            ("University",   d => d.University),
            ("Company Name", d => d.CompanyName),
        };

    public PromptExperimentService(
        IOllamaService ollama,
        IOptions<OllamaSettings> ollamaOptions,
        ILogger<PromptExperimentService> logger)
    {
        _ollama         = ollama         ?? throw new ArgumentNullException(nameof(ollama));
        _ollamaSettings = ollamaOptions?.Value ?? throw new ArgumentNullException(nameof(ollamaOptions));
        _logger         = logger         ?? throw new ArgumentNullException(nameof(logger));
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public async Task<PromptExperimentReport> RunExperimentsAsync(DocumentDetails document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        _logger.LogInformation("Starting prompt experiment for candidate '{Name}'.", document.FullName);

        var documentText = SerialiseDocument(document);

        // Run each strategy sequentially so Ollama isn't overloaded
        var results = new List<PromptExperimentResult>
        {
            await RunStrategyAsync(PromptStrategy.ZeroShot,       document, documentText),
            await RunStrategyAsync(PromptStrategy.FewShot,        document, documentText),
            await RunStrategyAsync(PromptStrategy.ChainOfThought, document, documentText),
        };

        var best  = results
            .Where(r => r.Succeeded)
            .OrderByDescending(r => r.ObservedAccuracy)
            .FirstOrDefault();

        var table      = BuildComparisonTable(results);
        var fullReport = BuildFullReport(document, results, table, best);

        _logger.LogInformation("Prompt experiment complete. Best strategy: {Strategy}.", best?.StrategyLabel ?? "N/A");

        return new PromptExperimentReport
        {
            ReportId     = Guid.NewGuid().ToString("D"),
            GeneratedOn  = DateTime.UtcNow,
            ModelUsed    = _ollamaSettings.ModelName,
            Results      = results,
            BestStrategy = best?.StrategyLabel ?? "N/A",
            ComparisonTable = table,
            FullReport   = fullReport
        };
    }

    // =========================================================================
    // Strategy runner
    // =========================================================================

    private async Task<PromptExperimentResult> RunStrategyAsync(
        PromptStrategy strategy,
        DocumentDetails document,
        string documentText)
    {
        string label  = StrategyLabel(strategy);
        string prompt = BuildPrompt(strategy, documentText);

        _logger.LogInformation("Running {Strategy} strategy.", label);

        var sw = Stopwatch.StartNew();
        string output;
        bool succeeded = true;
        string? errorMessage = null;

        try
        {
            output = await _ollama.GenerateAsync(prompt);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "{Strategy} strategy failed.", label);
            return new PromptExperimentResult
            {
                Strategy      = strategy,
                StrategyLabel = label,
                Prompt        = prompt,
                Output        = string.Empty,
                ExecutionTimeMs  = sw.Elapsed.TotalMilliseconds,
                ObservedAccuracy = 0,
                Succeeded     = false,
                ErrorMessage  = ex.Message
            };
        }

        sw.Stop();

        var fieldDetails = ScoreFields(document, output);
        double accuracy  = fieldDetails.Count > 0
            ? fieldDetails.Count(f => f.FoundInOutput) / (double)fieldDetails.Count * 100.0
            : 0;

        return new PromptExperimentResult
        {
            Strategy         = strategy,
            StrategyLabel    = label,
            Prompt           = prompt,
            Output           = output,
            ExecutionTimeMs  = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            ObservedAccuracy = Math.Round(accuracy, 2),
            Succeeded        = succeeded,
            ErrorMessage     = errorMessage,
            FieldAccuracyDetails = fieldDetails
        };
    }

    // =========================================================================
    // Prompt builders
    // =========================================================================

    private static string BuildPrompt(PromptStrategy strategy, string documentText) => strategy switch
    {
        PromptStrategy.ZeroShot       => BuildZeroShotPrompt(documentText),
        PromptStrategy.FewShot        => BuildFewShotPrompt(documentText),
        PromptStrategy.ChainOfThought => BuildChainOfThoughtPrompt(documentText),
        _                             => throw new ArgumentOutOfRangeException(nameof(strategy))
    };

    // ── Zero-shot ─────────────────────────────────────────────────────────────
    private static string BuildZeroShotPrompt(string documentText)
    {
        return $"""
You are an HR compliance analyst. Extract the following fields from the employee document below.
Return each field on its own line in the format:  Field Name: Value

Fields to extract:
- Full Name
- Date of Birth
- Aadhaar Number
- PAN Number
- Address
- Degree
- University
- Company Name

Employee Document:
{documentText}
""";
    }

    // ── Few-shot ──────────────────────────────────────────────────────────────
    private static string BuildFewShotPrompt(string documentText)
    {
        return $"""
You are an HR compliance analyst. Extract structured fields from employee documents.
Use the examples below to understand the expected output format, then extract from the new document.

---
EXAMPLE 1
Document:
  Name: Priya Sharma
  DOB: 15-Aug-1990
  Aadhaar: 1234 5678 9012
  PAN: ABCDE1234F
  Address: 12 MG Road, Bengaluru
  Degree: B.Tech Computer Science
  University: VTU
  Employer: Infosys Ltd.

Output:
  Full Name: Priya Sharma
  Date of Birth: 1990-08-15
  Aadhaar Number: 1234 5678 9012
  PAN Number: ABCDE1234F
  Address: 12 MG Road, Bengaluru
  Degree: B.Tech Computer Science
  University: VTU
  Company Name: Infosys Ltd.

---
EXAMPLE 2
Document:
  Candidate: Rahul Verma, born 02/03/1985
  ID: Aadhaar 9876 5432 1098 | PAN: VRMRH9876K
  Residence: Flat 4B, Andheri West, Mumbai 400058
  Education: MBA Finance, IIM Ahmedabad
  Last Employer: HDFC Bank

Output:
  Full Name: Rahul Verma
  Date of Birth: 1985-03-02
  Aadhaar Number: 9876 5432 1098
  PAN Number: VRMRH9876K
  Address: Flat 4B, Andheri West, Mumbai 400058
  Degree: MBA Finance
  University: IIM Ahmedabad
  Company Name: HDFC Bank

---
Now extract fields from this new employee document using the same output format:

Document:
{documentText}

Output:
""";
    }

    // ── Chain-of-thought ──────────────────────────────────────────────────────
    private static string BuildChainOfThoughtPrompt(string documentText)
    {
        return $"""
You are an HR compliance analyst. Your task is to extract structured fields from an employee document.
Think step by step before producing your final answer.

Follow these reasoning steps:
Step 1 – Read the entire document carefully and identify all named entities (people, places, organisations, identifiers).
Step 2 – Match each entity to the correct field: Full Name, Date of Birth, Aadhaar Number, PAN Number, Address, Degree, University, Company Name.
Step 3 – Normalise dates to ISO format (yyyy-MM-dd). Normalise Aadhaar to groups of 4 digits separated by spaces. PAN must be exactly 10 characters.
Step 4 – If a field is not present in the document, write "Not Found" for that field.
Step 5 – Output ONLY the final extracted fields in the format below. Do not include your reasoning steps in the output.

Output format (one field per line):
  Full Name: <value>
  Date of Birth: <value>
  Aadhaar Number: <value>
  PAN Number: <value>
  Address: <value>
  Degree: <value>
  University: <value>
  Company Name: <value>

Employee Document:
{documentText}
""";
    }

    // =========================================================================
    // Accuracy scoring
    // =========================================================================

    private static IReadOnlyList<FieldAccuracyDetail> ScoreFields(DocumentDetails doc, string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return TrackedFields
                .Select(f => new FieldAccuracyDetail { FieldName = f.Label, ExpectedValue = f.Get(doc), FoundInOutput = false })
                .ToList();
        }

        var lower = output.ToLowerInvariant();

        return TrackedFields
            .Select(f =>
            {
                var expected = f.Get(doc)?.Trim() ?? string.Empty;
                bool found   = !string.IsNullOrWhiteSpace(expected)
                               && lower.Contains(expected.ToLowerInvariant());

                return new FieldAccuracyDetail
                {
                    FieldName     = f.Label,
                    ExpectedValue = expected,
                    FoundInOutput = found
                };
            })
            .ToList();
    }

    // =========================================================================
    // Report builders
    // =========================================================================

    private static string BuildComparisonTable(IReadOnlyList<PromptExperimentResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Strategy Comparison Table");
        sb.AppendLine();
        sb.AppendLine("| Strategy | Execution Time (ms) | Observed Accuracy (%) | Succeeded | Fields Found |");
        sb.AppendLine("|----------|--------------------|-----------------------|-----------|--------------|");

        foreach (var r in results)
        {
            int found = r.FieldAccuracyDetails.Count(f => f.FoundInOutput);
            int total = r.FieldAccuracyDetails.Count;
            string fieldsFound = r.Succeeded ? $"{found}/{total}" : "—";
            string accuracy    = r.Succeeded ? $"{r.ObservedAccuracy:F1}" : "—";
            string execTime    = r.Succeeded ? $"{r.ExecutionTimeMs:F0}" : "—";
            string succeeded   = r.Succeeded ? "✅" : "❌";

            sb.AppendLine($"| **{r.StrategyLabel}** | {execTime} | {accuracy} | {succeeded} | {fieldsFound} |");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private static string BuildFullReport(
        DocumentDetails document,
        IReadOnlyList<PromptExperimentResult> results,
        string comparisonTable,
        PromptExperimentResult? best)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Prompt Strategy Experiment Report");
        sb.AppendLine($"**Generated On:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC  ");
        sb.AppendLine($"**Candidate:** {document.FullName}  ");
        sb.AppendLine($"**Strategies Evaluated:** Zero-Shot, Few-Shot, Chain-of-Thought  ");
        sb.AppendLine($"**Best Strategy:** {best?.StrategyLabel ?? "N/A"}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Comparison table
        sb.Append(comparisonTable);
        sb.AppendLine("---");
        sb.AppendLine();

        // Per-strategy detail
        sb.AppendLine("## Strategy Details");
        sb.AppendLine();

        foreach (var r in results)
        {
            sb.AppendLine($"### {r.StrategyLabel}");
            sb.AppendLine();

            if (!r.Succeeded)
            {
                sb.AppendLine($"> ❌ **Failed:** {r.ErrorMessage}");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"**Execution Time:** {r.ExecutionTimeMs:F0} ms  ");
            sb.AppendLine($"**Observed Accuracy:** {r.ObservedAccuracy:F1}%  ");
            sb.AppendLine();

            // Prompt (collapsed for readability)
            sb.AppendLine("<details>");
            sb.AppendLine($"<summary>View Prompt</summary>");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(r.Prompt);
            sb.AppendLine("```");
            sb.AppendLine("</details>");
            sb.AppendLine();

            // Output (collapsed for readability)
            sb.AppendLine("<details>");
            sb.AppendLine("<summary>View Model Output</summary>");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(r.Output);
            sb.AppendLine("```");
            sb.AppendLine("</details>");
            sb.AppendLine();

            // Field-level accuracy
            sb.AppendLine("#### Field Accuracy Breakdown");
            sb.AppendLine();
            sb.AppendLine("| Field | Expected Value | Found in Output |");
            sb.AppendLine("|-------|---------------|-----------------|");
            foreach (var f in r.FieldAccuracyDetails)
            {
                string expected = string.IsNullOrWhiteSpace(f.ExpectedValue) ? "_not provided_" : f.ExpectedValue;
                string found    = f.FoundInOutput ? "✅" : "❌";
                sb.AppendLine($"| {f.FieldName} | {expected} | {found} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();

        // Recommendation
        sb.AppendLine("## Recommendation");
        sb.AppendLine();
        AppendRecommendation(sb, results, best);

        return sb.ToString();
    }

    private static void AppendRecommendation(
        StringBuilder sb,
        IReadOnlyList<PromptExperimentResult> results,
        PromptExperimentResult? best)
    {
        if (best is null)
        {
            sb.AppendLine("All strategies failed to produce output. Verify that Ollama is running and the configured model is available.");
            return;
        }

        sb.AppendLine($"Based on the experiment, **{best.StrategyLabel}** is the recommended prompting strategy for this task.");
        sb.AppendLine();
        sb.AppendLine("#### Strategy Analysis");
        sb.AppendLine();

        var zeroShot = results.FirstOrDefault(r => r.Strategy == PromptStrategy.ZeroShot);
        var fewShot  = results.FirstOrDefault(r => r.Strategy == PromptStrategy.FewShot);
        var cot      = results.FirstOrDefault(r => r.Strategy == PromptStrategy.ChainOfThought);

        sb.AppendLine("| Strategy | Characteristics | When to Use |");
        sb.AppendLine("|----------|----------------|-------------|");
        sb.AppendLine("| **Zero-Shot** | No examples provided. Relies entirely on model's pre-trained knowledge. Fastest. | Use for simple, well-defined extraction tasks where the model understands the domain. |");
        sb.AppendLine("| **Few-Shot** | 2–3 labelled examples guide the format. Higher prompt token cost but better format adherence. | Use when output format consistency is critical or the model struggles zero-shot. |");
        sb.AppendLine("| **Chain-of-Thought** | Model is instructed to reason step by step before producing output. Highest accuracy for complex documents. | Use for ambiguous or richly structured documents where field boundaries are unclear. |");
        sb.AppendLine();

        sb.AppendLine("#### Accuracy Summary");
        sb.AppendLine();
        foreach (var r in results.Where(r => r.Succeeded))
        {
            sb.AppendLine($"- **{r.StrategyLabel}:** {r.ObservedAccuracy:F1}% accuracy in {r.ExecutionTimeMs:F0} ms");
        }
        sb.AppendLine();

        // Trade-off note
        var fastest = results.Where(r => r.Succeeded).OrderBy(r => r.ExecutionTimeMs).FirstOrDefault();
        var most_accurate = results.Where(r => r.Succeeded).OrderByDescending(r => r.ObservedAccuracy).FirstOrDefault();

        if (fastest != null && most_accurate != null && fastest.Strategy != most_accurate.Strategy)
        {
            sb.AppendLine($"> **Trade-off note:** **{fastest.StrategyLabel}** is the fastest ({fastest.ExecutionTimeMs:F0} ms) " +
                          $"while **{most_accurate.StrategyLabel}** achieves the highest accuracy ({most_accurate.ObservedAccuracy:F1}%). " +
                          $"Choose based on whether throughput or precision is the priority for your deployment.");
        }
        else if (best != null)
        {
            sb.AppendLine($"> **{best.StrategyLabel}** achieves both the best accuracy ({best.ObservedAccuracy:F1}%) and competitive execution time ({best.ExecutionTimeMs:F0} ms), making it the clear choice.");
        }
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string StrategyLabel(PromptStrategy strategy) => strategy switch
    {
        PromptStrategy.ZeroShot       => "Zero-Shot",
        PromptStrategy.FewShot        => "Few-Shot",
        PromptStrategy.ChainOfThought => "Chain-of-Thought",
        _                             => strategy.ToString()
    };

    /// <summary>Converts DocumentDetails into a realistic document text block.</summary>
    private static string SerialiseDocument(DocumentDetails d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Employee Background Document");
        sb.AppendLine("----------------------------");
        if (!string.IsNullOrWhiteSpace(d.FullName))        sb.AppendLine($"Name            : {d.FullName}");
        if (!string.IsNullOrWhiteSpace(d.DateOfBirth))     sb.AppendLine($"Date of Birth   : {d.DateOfBirth}");
        if (!string.IsNullOrWhiteSpace(d.AadhaarNumber))   sb.AppendLine($"Aadhaar         : {d.AadhaarNumber}");
        if (!string.IsNullOrWhiteSpace(d.PanNumber))       sb.AppendLine($"PAN             : {d.PanNumber}");
        if (!string.IsNullOrWhiteSpace(d.Address))         sb.AppendLine($"Address         : {d.Address}");
        if (!string.IsNullOrWhiteSpace(d.Degree))          sb.AppendLine($"Degree          : {d.Degree}");
        if (!string.IsNullOrWhiteSpace(d.University))      sb.AppendLine($"University      : {d.University}");
        if (!string.IsNullOrWhiteSpace(d.CompanyName))     sb.AppendLine($"Last Employer   : {d.CompanyName}");
        return sb.ToString();
    }
}
