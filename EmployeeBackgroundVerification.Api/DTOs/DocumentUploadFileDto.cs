namespace EmployeeBackgroundVerification.Api.DTOs;

public sealed class DocumentUploadFileDto
{
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
}
