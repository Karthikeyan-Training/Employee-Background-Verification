# Employee Background Verification API

## Overview

`EmployeeBackgroundVerification.Api` is an ASP.NET Core 8 Web API that provides a clean architecture for performing employee background verification and generating verification reports.

## Project Structure

- `Controllers/` - API controllers
- `Services/` - business service implementations and interfaces
- `Models/` - domain models and settings
- `DTOs/` - request and response objects
- `Helpers/` - mapping and utility extensions
- `Prompts/` - reserved for prompt templates or workflow definitions
- `Documents/` - reserved for related documentation
- `Reports/` - reserved for generated report content

## Features

- Swagger-enabled API documentation
- Dependency injection for services
- Appsettings-based configuration
- Clean SOLID-friendly architecture

## Configuration

The project uses `appsettings.json` for configuration. Example configuration values:

```json
{
  "BackgroundVerification": {
    "DefaultCheckLevel": "Standard",
    "ReportPath": "Reports"
  }
}
```

## Running the API

From the project folder:

```powershell
cd "EmployeeBackgroundVerification.Api"
dotnet run
```

## Swagger

When running in development, Swagger UI is available at:

```
https://localhost:<port>/swagger
```

## API Endpoint

### `POST /api/BackgroundVerification`

Request body:

```json
{
  "candidateName": "John Doe",
  "email": "john.doe@example.com",
  "position": "Software Engineer",
  "criminalRecordCheck": true
}
```

Response body:

```json
{
  "candidateName": "John Doe",
  "email": "john.doe@example.com",
  "verificationLevel": "Standard",
  "status": "Completed",
  "summary": "Verification completed using Standard checks.",
  "reportId": "...",
  "completedOn": "2026-06-30T00:00:00Z"
}
```

## Notes

- The project is designed for extension with persistence, validation, or external verification providers.
- `ReportPath` is a placeholder setting for storing generated report content.
