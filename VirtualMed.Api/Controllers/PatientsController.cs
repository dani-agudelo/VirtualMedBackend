using MediatR;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Application.Commands.Patients;
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

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePatientCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
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
