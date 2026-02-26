using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

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
        _logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var problem = new
        {
            traceId = httpContext.TraceIdentifier,
            status = httpContext.Response.StatusCode,
            title = "An unexpected error occurred.",
            detail = exception.Message
        };

        var json = JsonSerializer.Serialize(problem);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
}

