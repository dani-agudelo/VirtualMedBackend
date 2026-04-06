using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Authorization;
using VirtualMed.Application.Queries.Doctors;

namespace VirtualMed.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DoctorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DoctorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    [RequirePermission("Appointment", "Create")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new SearchDoctorsQuery(q, page, pageSize));
        return Ok(result);
    }
}
