namespace EmployeeBackgroundVerification.Api.Helpers;

using System;

public sealed class DocumentUploadException : Exception
{
    public DocumentUploadException(string message)
        : base(message)
    {
    }
}
