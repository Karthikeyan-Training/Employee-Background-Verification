using System;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services;
using EmployeeBackgroundVerification.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<BackgroundVerificationSettings>(builder.Configuration.GetSection("BackgroundVerification"));
builder.Services.Configure<DocumentUploadSettings>(builder.Configuration.GetSection("DocumentUpload"));
builder.Services.AddScoped<IBackgroundVerificationService, BackgroundVerificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDocumentStorageService, DocumentStorageService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection("Ollama"));

builder.Services.AddHttpClient<IOllamaService, OllamaService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<OllamaSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
    {
        client.BaseAddress = new Uri(opts.BaseUrl);
    }
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Document extraction service (uses Ollama)
builder.Services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
