using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Commands.Auth;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailTokenService _emailTokenService;
    private readonly INotificationService _notification;
    private readonly EmailSettings _emailSettings;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailTokenService emailTokenService,
        INotificationService notification,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _emailTokenService = emailTokenService;
        _notification = notification;
        _emailSettings = emailSettings.Value;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var user = await _context.Set<Domain.Entities.User>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || string.Equals(user.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
            return;

        var rawToken = await _emailTokenService.CreateTokenAsync(
            user.Id,
            UserEmailTokenType.PasswordReset,
            TimeSpan.FromMinutes(_emailSettings.PasswordResetMinutes),
            cancellationToken);

        await _notification.SendPasswordResetAsync(user, rawToken, cancellationToken);
    }
}
