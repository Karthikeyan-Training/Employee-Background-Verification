namespace EmployeeBackgroundVerification.Api.Services.Interfaces;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmployeeBackgroundVerification.Api.DTOs;
using Microsoft.AspNetCore.Http;

public interface IDocumentStorageService
{
    Task<IEnumerable<DocumentUploadFileDto>> SaveFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default);
}
