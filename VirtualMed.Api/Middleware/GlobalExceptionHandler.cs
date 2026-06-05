using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using VirtualMed.Api.Models;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Exceptions;
using AppInvalidOperationException = VirtualMed.Application.Exceptions.InvalidOperationException;

namespace VirtualMed.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, message) = MapException(exception);

        if (statusCode == 500)
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", httpContext.TraceIdentifier);
        else
            _logger.LogWarning(exception, "Handled exception. Code: {Code}, TraceId: {TraceId}", code, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow.ToString("O"),
            Code = code,
            Message = message,
            TraceId = httpContext.TraceIdentifier,
            Errors = exception is ValidationException ve ? MapValidationErrors(ve) : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }

    private static (int statusCode, string code, string message) MapException(Exception exception)
    {
        return exception switch
        {
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } =>
                (413, "PAYLOAD_TOO_LARGE", "El archivo supera el limite permitido de subida."),
            BadHttpRequestException badHttp =>
                (400, "BAD_REQUEST", badHttp.Message),
            InvalidDataException =>
                (400, "INVALID_UPLOAD", "El archivo supera el limite permitido de subida."),
            ValidationException => (400, "VALIDATION_ERROR", "Datos de entrada inválidos"),
            UnauthorizedAccessException => (401, "UNAUTHORIZED", exception.Message),
            ForbiddenException => (403, "FORBIDDEN", exception.Message),
            NotFoundException => (404, "NOT_FOUND", exception.Message),
            DuplicateEntityException => (409, "CONFLICT", exception.Message),
            AppInvalidOperationException => (409, "BUSINESS_ERROR", exception.Message),
            BusinessRuleException bre => (409, bre.ErrorCode ?? "BUSINESS_ERROR", bre.Message),
            ExternalServiceException ese => (503, "EXTERNAL_SERVICE_ERROR", ese.Message),
            _ => (500, "INTERNAL_ERROR", "Ha ocurrido un error interno. Use el traceId para soporte.")
        };
    }

    private static IReadOnlyList<ValidationErrorDetail> MapValidationErrors(ValidationException ve)
    {
        return ve.Errors
            .Select(e => new ValidationErrorDetail
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            })
            .ToList();
    }
}
