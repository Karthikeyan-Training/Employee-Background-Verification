using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

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
