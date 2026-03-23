using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Application.Queries.AuditLogs;

namespace VirtualMed.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private bool IsAdmin()
    {
        var role = User.FindFirst("role")?.Value;
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? tableName,
        [FromQuery] string? operation,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!IsAdmin())
            return Forbid();

        var result = await _mediator.Send(new GetAuditLogsQuery(
            tableName,
            operation,
            from,
            to,
            pageNumber,
            pageSize));

        return Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? tableName,
        [FromQuery] string? operation,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int maxRows = 5000)
    {
        if (!IsAdmin())
            return Forbid();

        var csv = await _mediator.Send(new ExportAuditLogsQuery(
            tableName,
            operation,
            from,
            to,
            maxRows <= 0 ? 5000 : maxRows));

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }
}

