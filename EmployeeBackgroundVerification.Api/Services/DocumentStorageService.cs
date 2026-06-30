namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using EmployeeBackgroundVerification.Api.DTOs;
using EmployeeBackgroundVerification.Api.Helpers;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

public class DocumentStorageService : IDocumentStorageService
{
    private static readonly string[] AllowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
    private readonly DocumentUploadSettings _settings;
    private readonly string _documentsRootPath;

    public DocumentStorageService(IOptions<DocumentUploadSettings> options, IHostEnvironment hostEnvironment)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (hostEnvironment is null)
        {
            throw new ArgumentNullException(nameof(hostEnvironment));
        }

        _documentsRootPath = Path.Combine(hostEnvironment.ContentRootPath, _settings.DocumentFolderPath);
    }

    public async Task<IEnumerable<DocumentUploadFileDto>> SaveFilesAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken = default)
    {
        if (files is null)
        {
            throw new DocumentUploadException("No files were provided for upload.");
        }

        var validFiles = files.Where(file => file is not null && file.Length > 0).ToList();
        if (!validFiles.Any())
        {
            throw new DocumentUploadException("At least one non-empty file is required.");
        }

        Directory.CreateDirectory(_documentsRootPath);

        var savedFiles = new List<DocumentUploadFileDto>();
        foreach (var file in validFiles)
        {
            ValidateFile(file);

            var originalFileName = Path.GetFileName(file.FileName) ?? throw new DocumentUploadException("File name is invalid.");
            var storageFileName = GetUniqueFileName(originalFileName);
            var storagePath = Path.Combine(_documentsRootPath, storageFileName);

            try
            {
                await using var stream = new FileStream(storagePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
                await file.CopyToAsync(stream, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DocumentUploadException($"Failed to save file '{originalFileName}': {ex.Message}");
            }

            savedFiles.Add(new DocumentUploadFileDto
            {
                FileName = originalFileName,
                FilePath = Path.Combine(_settings.DocumentFolderPath, storageFileName).Replace("\\", "/")
            });
        }

        return savedFiles;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new DocumentUploadException($"File '{file.FileName}' is empty.");
        }

        if (file.Length > _settings.MaxFileSizeInBytes)
        {
            throw new DocumentUploadException($"File '{file.FileName}' exceeds the maximum allowed size of {_settings.MaxFileSizeInBytes} bytes.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new DocumentUploadException($"File '{file.FileName}' has an unsupported file type. Allowed extensions are: {string.Join(", ", AllowedExtensions)}.");
        }
    }

    private static string GetUniqueFileName(string originalFileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        return $"{fileNameWithoutExtension}_{timestamp}{extension}";
    }
}
