using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Models.Auth;
using VirtualMed.Application.Commands.Auth;
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

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> EnableTwoFactor()
        {
            var userId = GetUserId();
            var result = await _mediator.Send(new EnableTwoFactorCommand(userId));
            return Ok(new
            {
                otpauthUri = result.OtpauthUri,
                secret = result.Secret,
                recoveryCodes = result.RecoveryCodes
            });
        }

        [Authorize]
        [HttpPost("2fa/verify")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
        {
            var userId = GetUserId();
            await _mediator.Send(new VerifyTwoFactorCodeCommand(userId, request.Code));
            return NoContent();
        }

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
        {
            var userId = GetUserId();
            await _mediator.Send(new DisableTwoFactorCommand(userId, request.RecoveryCode));
            return NoContent();
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("No se pudo determinar el usuario autenticado.");

            return userId;
        }
    }
}

