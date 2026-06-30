using System.Collections.Generic;

namespace EmployeeBackgroundVerification.Api.Models;

public class FraudDetectionSettings
{
    public List<string> MandatoryDocumentNames { get; set; } = new();
    public List<string> SuspiciousAadhaarPatterns { get; set; } = new();
    public List<string> SuspiciousPanPatterns { get; set; } = new();
    public int DuplicateDocumentNumberThreshold { get; set; } = 2;
}
