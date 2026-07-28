namespace EmployeeBackgroundVerification.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.DTOs;
using EmployeeBackgroundVerification.Api.Helpers;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/demo")]
public class DemoController : ControllerBase
{
    private readonly IDocumentStorageService _documentStorage;
    private readonly IOcrService _ocr;
    private readonly IDocumentExtractionService _documentExtraction;
    private readonly IVerificationService _verification;
    private readonly IFraudDetectionService _fraudDetection;
    private readonly IRiskScoringService _riskScoring;
    private readonly IReportGenerationService _reportGeneration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<DemoController> _logger;

    public DemoController(
        IDocumentStorageService documentStorage,
        IOcrService ocr,
        IDocumentExtractionService documentExtraction,
        IVerificationService verification,
        IFraudDetectionService fraudDetection,
        IRiskScoringService riskScoring,
        IReportGenerationService reportGeneration,
        IHostEnvironment hostEnvironment,
        ILogger<DemoController> logger)
    {
        _documentStorage   = documentStorage   ?? throw new ArgumentNullException(nameof(documentStorage));
        _ocr               = ocr               ?? throw new ArgumentNullException(nameof(ocr));
        _documentExtraction = documentExtraction ?? throw new ArgumentNullException(nameof(documentExtraction));
        _verification      = verification      ?? throw new ArgumentNullException(nameof(verification));
        _fraudDetection    = fraudDetection    ?? throw new ArgumentNullException(nameof(fraudDetection));
        _riskScoring       = riskScoring       ?? throw new ArgumentNullException(nameof(riskScoring));
        _reportGeneration  = reportGeneration  ?? throw new ArgumentNullException(nameof(reportGeneration));
        _hostEnvironment   = hostEnvironment   ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the full employee background verification pipeline end-to-end:
    /// upload → OCR → LLM extraction → verification → fraud detection → risk scoring → report generation.
    /// </summary>
    /// <param name="files">One or more employee documents (PDF, JPG, PNG; max 10 MB each).</param>
    /// <param name="candidateName">Candidate's full name.</param>
    /// <param name="email">Candidate's email address.</param>
    /// <param name="position">Position the candidate is applying for.</param>
    /// <param name="criminalRecordCheck">Whether to flag criminal record checks in the report.</param>
    [HttpPost("run")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    [ProducesResponseType(typeof(DemoRunResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DemoRunResponseDto>> RunAsync(
        [FromForm] IEnumerable<IFormFile> files,
        [FromForm] string candidateName,
        [FromForm] string email,
        [FromForm] string position,
        [FromForm] bool criminalRecordCheck = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(candidateName))
            return BadRequest(new { error = "candidateName is required." });

        var totalSw    = Stopwatch.StartNew();
        var steps      = new List<PipelineStepDto>();
        var docResults = new List<DocumentProcessingResultDto>();

        _logger.LogInformation("Demo run started for candidate '{Candidate}'.", candidateName);

        // ── Step 1: Document Upload ───────────────────────────────────────────
        IEnumerable<DocumentUploadFileDto> savedFiles;
        var sw = Stopwatch.StartNew();
        try
        {
            savedFiles = await _documentStorage.SaveFilesAsync(files, cancellationToken);
            steps.Add(Step("1. Document Upload", "Completed", sw, $"{savedFiles.Count()} file(s) saved"));
        }
        catch (Exception ex)
        {
            steps.Add(Step("1. Document Upload", "Failed", sw, ex.Message));
            _logger.LogError(ex, "Document upload failed.");
            return BadRequest(new { error = $"Document upload failed: {ex.Message}" });
        }

        // ── Step 2: OCR + LLM Extraction (per document) ──────────────────────
        var documentSources = new List<DocumentSource>();

        foreach (var saved in savedFiles)
        {
            var absolutePath = Path.Combine(_hostEnvironment.ContentRootPath, saved.FilePath);

            // OCR
            string ocrText;
            var ocrSw = Stopwatch.StartNew();
            try
            {
                ocrText = await _ocr.ExtractTextAsync(absolutePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR failed for '{File}'.", saved.FileName);
                ocrText = string.Empty;
            }
            ocrSw.Stop();

            // LLM Extraction
            DocumentDetails details;
            var extractSw = Stopwatch.StartNew();
            try
            {
                details = await _documentExtraction.ExtractAsync(ocrText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Document extraction failed for '{File}'.", saved.FileName);
                details = new DocumentDetails { FullName = candidateName };
            }
            extractSw.Stop();

            docResults.Add(new DocumentProcessingResultDto
            {
                FileName           = saved.FileName,
                OcrText            = ocrText,
                OcrDurationMs      = ocrSw.Elapsed.TotalMilliseconds,
                ExtractionDurationMs = extractSw.Elapsed.TotalMilliseconds,
                ExtractedFields    = MapToExtractedDto(details)
            });

            documentSources.Add(new DocumentSource
            {
                SourceName = Path.GetFileNameWithoutExtension(saved.FileName),
                Details    = details
            });
        }

        steps.Add(Step("2. OCR Extraction", "Completed", sw,
            $"{docResults.Count} document(s) processed"));

        steps.Add(Step("3. LLM Field Extraction", "Completed", sw,
            $"Extracted fields from {documentSources.Count} document source(s)"));

        // ── Step 4: Cross-Document Verification ───────────────────────────────
        VerificationResult verificationResult;
        sw = Stopwatch.StartNew();
        try
        {
            verificationResult = await _verification.VerifyAsync(documentSources);
            steps.Add(Step("4. Document Verification", "Completed", sw,
                $"Status: {verificationResult.Status} · Mismatches: {verificationResult.MismatchedFields.Count}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verification step failed.");
            steps.Add(Step("4. Document Verification", "Failed", sw, ex.Message));
            verificationResult = new VerificationResult { Status = VerificationStatus.Unverified };
        }

        // ── Step 5: Fraud Detection ────────────────────────────────────────────
        FraudResult fraudResult;
        sw = Stopwatch.StartNew();
        try
        {
            fraudResult = await _fraudDetection.AnalyzeAsync(documentSources);
            steps.Add(Step("5. Fraud Detection", "Completed", sw,
                $"Fraud detected: {fraudResult.IsFraudulent} · Severity: {fraudResult.MaxSeverity}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fraud detection step failed.");
            steps.Add(Step("5. Fraud Detection", "Failed", sw, ex.Message));
            fraudResult = new FraudResult();
        }

        // ── Step 6: Risk Scoring ───────────────────────────────────────────────
        sw = Stopwatch.StartNew();
        var riskInput = new RiskScoringInput
        {
            NameMismatch      = verificationResult.MismatchedFields.Any(m =>
                                    m.FieldName.Contains("Name", StringComparison.OrdinalIgnoreCase)),
            DobMismatch       = verificationResult.MismatchedFields.Any(m =>
                                    m.FieldName.Contains("Birth", StringComparison.OrdinalIgnoreCase) ||
                                    m.FieldName.Contains("DOB",   StringComparison.OrdinalIgnoreCase)),
            IsPanMissing      = verificationResult.MissingFields.Any(f =>
                                    f.Contains("PAN", StringComparison.OrdinalIgnoreCase)),
            IsAadhaarMissing  = verificationResult.MissingFields.Any(f =>
                                    f.Contains("Aadhaar", StringComparison.OrdinalIgnoreCase)),
            FraudDetected     = fraudResult.IsFraudulent
        };

        var riskAssessment = _riskScoring.Assess(riskInput);
        steps.Add(Step("6. Risk Scoring", "Completed", sw,
            $"Score: {riskAssessment.Score}/100 · Level: {riskAssessment.Level}"));

        // ── Step 7: Report Generation ─────────────────────────────────────────
        var candidateRequest = new BackgroundVerificationRequest
        {
            CandidateName      = candidateName,
            Email              = email ?? string.Empty,
            Position           = position ?? string.Empty,
            CriminalRecordCheck = criminalRecordCheck
        };

        sw = Stopwatch.StartNew();
        ReportResult report;
        try
        {
            report = await _reportGeneration.GenerateAsync(
                candidateRequest,
                verificationResult,
                fraudResult,
                riskAssessment.Score,
                riskAssessment.Level);
            steps.Add(Step("7. Report Generation", "Completed", sw,
                $"Report ID: {report.ReportId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report generation failed.");
            steps.Add(Step("7. Report Generation", "Failed", sw, ex.Message));
            report = new ReportResult
            {
                ReportId = Guid.NewGuid().ToString("D"),
                Content  = $"Report generation failed: {ex.Message}"
            };
        }

        totalSw.Stop();
        _logger.LogInformation("Demo run completed for '{Candidate}' in {Ms:F0} ms.", candidateName, totalSw.Elapsed.TotalMilliseconds);

        var response = new DemoRunResponseDto
        {
            CandidateName  = candidateName,
            Email          = email          ?? string.Empty,
            Position       = position       ?? string.Empty,
            PipelineSteps  = steps,
            Documents      = docResults,
            Verification   = MapVerification(verificationResult),
            Fraud          = MapFraud(fraudResult),
            Risk           = new RiskSummaryDto
            {
                Score          = riskAssessment.Score,
                Level          = riskAssessment.Level,
                Recommendation = riskAssessment.Recommendation
            },
            ReportId       = report.ReportId,
            ReportContent  = report.Content,
            CompletedOn    = DateTime.UtcNow,
            TotalDurationMs = totalSw.Elapsed.TotalMilliseconds
        };

        return Ok(response);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static PipelineStepDto Step(string name, string status, Stopwatch sw, string? detail = null)
    {
        sw.Stop();
        var result = new PipelineStepDto
        {
            Step       = name,
            Status     = status,
            DurationMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2),
            Detail     = detail
        };
        sw.Restart();
        return result;
    }

    private static ExtractedDocumentDto MapToExtractedDto(DocumentDetails d) => new()
    {
        FullName      = d.FullName,
        DateOfBirth   = d.DateOfBirth,
        AadhaarNumber = d.AadhaarNumber,
        PanNumber     = d.PanNumber,
        Address       = d.Address,
        Degree        = d.Degree,
        University    = d.University,
        CompanyName   = d.CompanyName
    };

    private static VerificationSummaryDto MapVerification(VerificationResult v) => new()
    {
        Status          = v.Status.ToString(),
        MatchedFields   = v.MatchedFields,
        MismatchedFields = v.MismatchedFields
            .Select(m => new MismatchedFieldDto
            {
                FieldName      = m.FieldName,
                ValuesBySource = m.ValuesBySource
            })
            .ToList(),
        MissingFields   = v.MissingFields,
        Warnings        = v.Warnings
    };

    private static FraudSummaryDto MapFraud(FraudResult f) => new()
    {
        IsFraudulent  = f.IsFraudulent,
        MaxSeverity   = f.MaxSeverity.ToString(),
        Indicators    = f.Indicators
            .Select(i => new FraudIndicatorDto
            {
                IndicatorName = i.IndicatorName,
                Severity      = i.Severity.ToString(),
                Description   = i.Description
            })
            .ToList(),
        ManualReviewRecommendations = f.ManualReviewRecommendations
    };
}
