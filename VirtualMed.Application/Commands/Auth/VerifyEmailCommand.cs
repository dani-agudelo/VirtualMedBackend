using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Commands.Auth;

public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailTokenService _emailTokenService;

    public VerifyEmailCommandHandler(IApplicationDbContext context, IEmailTokenService emailTokenService)
    {
        _context = context;
        _emailTokenService = emailTokenService;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _emailTokenService.ValidateAndConsumeTokenAsync(
            request.Token,
            UserEmailTokenType.EmailVerification,
            cancellationToken);

        if (user is null)
            throw new BusinessRuleException("EMAIL_TOKEN_INVALID", "El enlace de verificación es inválido o expiró.");

        user.EmailVerified = true;

        if (user.Status == "Pending")
            user.Status = "Active";

        _context.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
