using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Authorization;
using VirtualMed.Application.Commands.ClinicalEncounters;
using VirtualMed.Application.Queries.ClinicalEncounters;

namespace VirtualMed.Api.Controllers;

[ApiController]
[Route("api/clinical-encounters")]
[Authorize]
public class ClinicalEncountersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClinicalEncountersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequirePermission("ClinicalEncounter", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateClinicalEncounterCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("patient/{patientId:guid}")]
    [RequirePermission("ClinicalEncounter", "Read")]
    public async Task<IActionResult> GetPatientHistory(
        Guid patientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _mediator.Send(new GetPatientHistoryQuery(patientId, from, to));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("ClinicalEncounter", "Read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetClinicalEncounterByIdQuery(id));
        return Ok(result);
    }
}

