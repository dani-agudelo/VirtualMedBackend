using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using VirtualMed.Api.Models;
using VirtualMed.Application.Common.Exceptions;

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
            ValidationException => (400, "VALIDATION_ERROR", "Datos de entrada inválidos"),
            UnauthorizedAccessException => (401, "UNAUTHORIZED", "Acceso no autorizado"),
            BusinessRuleException => (409, "BUSINESS_ERROR", exception.Message),
            ExternalServiceException => (503, "EXTERNAL_SERVICE_ERROR", "Servicio externo no disponible temporalmente"),
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
