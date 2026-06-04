using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualMed.Api.Models.Auth;
using VirtualMed.Application.Commands.Auth;
using VirtualMed.Application.Commands.Doctors;
using VirtualMed.Application.Commands.Patients;

namespace VirtualMed.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
            if (result.RequiresTwoFactor)
                return Ok(new { requiresTwoFactor = true, tempTwoFactorToken = result.TempTwoFactorToken });
            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresInSeconds = result.ExpiresInSeconds
            });
        }

        [HttpPost("login/2fa")]
        public async Task<IActionResult> CompleteTwoFactorLogin([FromBody] CompleteTwoFactorLoginRequest request)
        {
            var result = await _mediator.Send(new CompleteTwoFactorLoginCommand(request.TempTwoFactorToken, request.Code));
            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresInSeconds = result.ExpiresInSeconds
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken));
            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                expiresInSeconds = result.ExpiresInSeconds
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
        {
            var userId = GetUserId();
            await _mediator.Send(new LogoutCommand(userId, request?.RefreshToken));
            return NoContent();
        }

        [HttpPost("register/doctor")]
        public async Task<IActionResult> RegisterDoctor(
            [FromForm] RegisterDoctorCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { doctorId = id });
        }

        [HttpPost("register/patient")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        public async Task<IActionResult> RegisterPatient([FromBody] CreatePatientCommand command)
        {
            var id = await _mediator.Send(command);
            return CreatedAtAction(
                nameof(PatientsController.GetById),
                "Patients",
                new { id },
                new { patientId = id });
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

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            await _mediator.Send(new VerifyEmailCommand(request.Token));
            return Ok(new { message = "Correo verificado correctamente." });
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification([FromBody] ResendEmailVerificationRequest request)
        {
            await _mediator.Send(new ResendEmailVerificationCommand(request.Email));
            return Ok(new { message = "Si el correo existe y no está verificado, enviaremos un nuevo enlace." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _mediator.Send(new ForgotPasswordCommand(request.Email));
            return Ok(new { message = "Si el correo existe, enviaremos instrucciones para restablecer la contraseña." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst("sub") ??
                        User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null || !Guid.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("No se pudo determinar el usuario autenticado.");

            return userId;
        }
    }
}

