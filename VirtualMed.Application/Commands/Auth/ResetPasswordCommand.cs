using MediatR;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Commands.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailTokenService _emailTokenService;
    private readonly INotificationService _notification;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailTokenService emailTokenService,
        INotificationService notification)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailTokenService = emailTokenService;
        _notification = notification;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _emailTokenService.ValidateAndConsumeTokenAsync(
            request.Token,
            UserEmailTokenType.PasswordReset,
            cancellationToken);

        if (user is null)
            throw new BusinessRuleException("PASSWORD_RESET_TOKEN_INVALID", "El enlace de restablecimiento es inválido o expiró.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        _context.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        await _notification.SendPasswordChangedAsync(user, cancellationToken);
    }
}
