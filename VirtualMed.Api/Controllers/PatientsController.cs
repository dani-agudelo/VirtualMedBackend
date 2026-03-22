using MediatR;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Application.Queries.Patients;

namespace VirtualMed.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetPatientByIdQuery(id));
        if (result == null)
            return NotFound();
        return Ok(result);
    }
}
