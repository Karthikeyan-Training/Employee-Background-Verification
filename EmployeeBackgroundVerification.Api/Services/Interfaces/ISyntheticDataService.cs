namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Collections.Generic;
using EmployeeBackgroundVerification.Api.Models;

public interface ISyntheticDataService
{
    /// <summary>
    /// Returns the full catalogue of synthetic employee records.
    /// Each record contains structured fields plus matching simulated
    /// Resume, PAN card, and Aadhaar card document texts.
    /// </summary>
    IReadOnlyList<SyntheticEmployee> GetAll();

    /// <summary>Returns a single synthetic employee by 1-based ID (1–20).</summary>
    SyntheticEmployee? GetById(int id);
}
