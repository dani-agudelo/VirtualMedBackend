using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Authorization;
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

    [HttpGet("{patientId:guid}/export/fhir")]
    [Authorize]
    [RequirePermission("ClinicalEncounter", "Read")]
    public async Task<IActionResult> ExportFhirBundle(Guid patientId)
    {
        var json = await _mediator.Send(new ExportPatientFhirBundleQuery(patientId));
        var fileName = $"patient-{patientId}-history.fhir.json";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/fhir+json", fileName);
    }

    [HttpGet("{patientId:guid}/export/history/pdf")]
    [Authorize]
    [RequirePermission("ClinicalEncounter", "Read")]
    public async Task<IActionResult> ExportClinicalHistoryPdf(Guid patientId)
    {
        var pdf = await _mediator.Send(new ExportPatientClinicalHistoryPdfQuery(patientId));
        var fileName = $"historial-clinico-{patientId:N}-{DateTime.UtcNow:yyyyMMdd}.pdf";
        return File(pdf, "application/pdf", fileName);
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
