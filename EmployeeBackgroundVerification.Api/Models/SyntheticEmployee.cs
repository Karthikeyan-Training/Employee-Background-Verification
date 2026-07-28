namespace EmployeeBackgroundVerification.Api.Models;

/// <summary>
/// A fully synthetic employee record for testing purposes.
/// All values are fictitious; no real personal information is used.
/// </summary>
public sealed class SyntheticEmployee
{
    /// <summary>Sequential identifier (1–20).</summary>
    public int Id { get; init; }

    // ── Core fields ───────────────────────────────────────────────────────────

    public string Name { get; init; } = string.Empty;

    /// <summary>ISO 8601 date of birth (yyyy-MM-dd).</summary>
    public string DateOfBirth { get; init; } = string.Empty;

    /// <summary>Synthetic PAN in valid format (TSYNT + 4 digits + A). Not a real PAN.</summary>
    public string PanNumber { get; init; } = string.Empty;

    /// <summary>Synthetic Aadhaar in valid 12-digit format (9999 0000 XXXX). Not a real Aadhaar.</summary>
    public string AadhaarNumber { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;
    public string Degree { get; init; } = string.Empty;
    public string University { get; init; } = string.Empty;
    public string PreviousCompany { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;

    // ── Simulated document texts (as if produced by OCR) ─────────────────────

    /// <summary>Plain-text content of the synthetic résumé document.</summary>
    public string ResumeText { get; init; } = string.Empty;

    /// <summary>Plain-text content of the synthetic PAN card document.</summary>
    public string PanCardText { get; init; } = string.Empty;

    /// <summary>Plain-text content of the synthetic Aadhaar card document.</summary>
    public string AadhaarCardText { get; init; } = string.Empty;
}
