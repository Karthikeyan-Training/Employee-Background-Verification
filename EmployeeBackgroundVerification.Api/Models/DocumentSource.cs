namespace EmployeeBackgroundVerification.Api.Models;

public class DocumentSource
{
    public string SourceName { get; set; } = string.Empty;
    public DocumentDetails Details { get; set; } = new DocumentDetails();
}
