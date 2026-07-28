namespace EmployeeBackgroundVerification.Api.Models;

using System;

/// <summary>Result returned after the project report has been generated and saved.</summary>
public sealed class ProjectReportResult
{
    /// <summary>Unique identifier for this report run.</summary>
    public string ReportId { get; init; } = string.Empty;

    /// <summary>UTC timestamp when the report was generated.</summary>
    public DateTime GeneratedOn { get; init; }

    /// <summary>Absolute path of the saved Markdown file inside the Reports folder.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>File name only (e.g. project-report-20260728-143022.md).</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Full Markdown content of the report.</summary>
    public string Content { get; init; } = string.Empty;
}
