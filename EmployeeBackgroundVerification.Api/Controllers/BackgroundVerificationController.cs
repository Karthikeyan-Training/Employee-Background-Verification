namespace EmployeeBackgroundVerification.Api.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EmployeeBackgroundVerification.Api.DTOs;
using EmployeeBackgroundVerification.Api.Helpers;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class BackgroundVerificationController : ControllerBase
{
    private readonly IBackgroundVerificationService _verificationService;
    private readonly IReportService _reportService;

    public BackgroundVerificationController(
        IBackgroundVerificationService verificationService,
        IReportService reportService)
    {
        _verificationService = verificationService;
        _reportService = reportService;
    }

    [HttpPost]
    public async Task<ActionResult<BackgroundVerificationResponseDto>> PostAsync(BackgroundVerificationRequestDto request)
    {
        if (request is null)
        {
            return BadRequest("Request payload is required.");
        }

        var domainRequest = request.ToDomain();
        var verificationResult = await _verificationService.VerifyAsync(domainRequest);
        var report = await _reportService.GenerateReportAsync(verificationResult);

        var response = verificationResult.ToDto();
        response.ReportId = report.ReportId;

        return Ok(response);
    }
}
