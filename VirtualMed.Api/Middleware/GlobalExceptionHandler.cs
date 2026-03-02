using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using VirtualMed.Application.Exceptions;

namespace VirtualMed.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Recurso no encontrado"),
            DuplicateEntityException => (HttpStatusCode.Conflict, "Conflicto de duplicación"),
            VirtualMed.Application.Exceptions.InvalidOperationException => (HttpStatusCode.BadRequest, "Operación inválida"),
            FluentValidation.ValidationException => (HttpStatusCode.BadRequest, "Error de validación"),
            _ => (HttpStatusCode.InternalServerError, "Error interno del servidor")
        };

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        var problem = new
        {
            traceId = httpContext.TraceIdentifier,
            status = (int)statusCode,
            title = title,
            detail = exception.Message
        };

        var json = JsonSerializer.Serialize(problem);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
}
