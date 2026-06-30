namespace EmployeeBackgroundVerification.Api.Models;

public sealed class DocumentUploadSettings
{
    public string DocumentFolderPath { get; init; } = "Documents";
    public long MaxFileSizeInBytes { get; init; } = 10 * 1024 * 1024;
}
