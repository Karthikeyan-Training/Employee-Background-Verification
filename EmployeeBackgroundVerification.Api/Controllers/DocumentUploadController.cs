namespace EmployeeBackgroundVerification.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EmployeeBackgroundVerification.Api.DTOs;
using EmployeeBackgroundVerification.Api.Helpers;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

[ApiController]
[Route("api/document")]
public class DocumentUploadController : ControllerBase
{
    private readonly IDocumentStorageService _documentStorageService;

    public DocumentUploadController(IDocumentStorageService documentStorageService)
    {
        _documentStorageService = documentStorageService ?? throw new ArgumentNullException(nameof(documentStorageService));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<DocumentUploadResponseDto>> UploadAsync([FromForm] IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        try
        {
            var uploadedFiles = await _documentStorageService.SaveFilesAsync(files, cancellationToken);
            var response = new DocumentUploadResponseDto
            {
                UploadedFiles = uploadedFiles
            };

            return Ok(response);
        }
        catch (DocumentUploadException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
