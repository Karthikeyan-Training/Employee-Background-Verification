namespace EmployeeBackgroundVerification.Api.Models;

public class DocumentDetails
{
    public string FullName { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty; // ISO yyyy-MM-dd where possible
    public string Address { get; set; } = string.Empty;
    public string AadhaarNumber { get; set; } = string.Empty;
    public string PanNumber { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
