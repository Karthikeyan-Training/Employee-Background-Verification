namespace EmployeeBackgroundVerification.Api.DTOs;

using System.Collections.Generic;

public sealed class DocumentUploadResponseDto
{
    public IEnumerable<DocumentUploadFileDto> UploadedFiles { get; init; } = System.Array.Empty<DocumentUploadFileDto>();
}
