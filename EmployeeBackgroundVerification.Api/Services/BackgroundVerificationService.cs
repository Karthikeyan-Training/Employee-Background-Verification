namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class BackgroundVerificationService : IBackgroundVerificationService
{
    private readonly BackgroundVerificationSettings _settings;

    public BackgroundVerificationService(IOptions<BackgroundVerificationSettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<BackgroundVerificationResult> VerifyAsync(BackgroundVerificationRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var status = request.CriminalRecordCheck
            ? "Completed"
            : "Pending";

        var result = new BackgroundVerificationResult
        {
            CandidateName = request.CandidateName,
            Email = request.Email,
            VerificationLevel = _settings.DefaultCheckLevel,
            CompletedOn = DateTime.UtcNow,
            Status = status,
            Summary = $"Verification completed using {_settings.DefaultCheckLevel} checks."
        };

        return Task.FromResult(result);
    }
}
