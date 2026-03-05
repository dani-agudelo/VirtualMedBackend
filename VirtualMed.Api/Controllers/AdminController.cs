using MediatR;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Authorization;
using VirtualMed.Application.Commands.Doctors;

namespace VirtualMed.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Doctor", "Approve")]
    [HttpPost("doctors/{id}/approve")]
    public async Task<IActionResult> ApproveDoctor(Guid id)
    {
        var command = new ApproveDoctorCommand { DoctorId = id };
        await _mediator.Send(command);
        return Ok(new { message = "Doctor aprobado exitosamente." });
    }
}
