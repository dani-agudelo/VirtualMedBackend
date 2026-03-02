using MediatR;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Application.Commands.Doctors;

namespace VirtualMed.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register/doctor")]
        public async Task<IActionResult> RegisterDoctor(
            [FromForm] RegisterDoctorCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { doctorId = id });
        }
    }
}
