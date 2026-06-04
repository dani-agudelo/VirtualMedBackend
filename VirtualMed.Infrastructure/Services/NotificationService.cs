using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtualMed.Application.Configuration;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly EmailSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        IOptions<EmailSettings> settings,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task NotifyAdminAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminNotificationEmail))
        {
            _logger.LogInformation("[NOTIFICACIÓN ADMIN]: {Message}", message);
            return;
        }

        await SafeSendAsync(
            () => _emailService.SendAsync(
                _settings.AdminNotificationEmail!,
                "Administrador VirtualMed",
                "Notificación administrativa — VirtualMed",
                EmailTemplates.AdminNotification(message),
                cancellationToken),
            "NotifyAdminAsync");
    }

    public Task SendEmailVerificationAsync(User user, string rawToken, CancellationToken cancellationToken = default)
    {
        var url = BuildFrontendUrl($"/verify-email?token={Uri.EscapeDataString(rawToken)}");
        return SafeSendAsync(
            () => _emailService.SendAsync(
                user.Email,
                user.FullName,
                "Verifica tu correo — VirtualMed",
                EmailTemplates.EmailVerification(user.FullName, url),
                cancellationToken),
            "SendEmailVerificationAsync");
    }

    public Task SendPasswordResetAsync(User user, string rawToken, CancellationToken cancellationToken = default)
    {
        var url = BuildFrontendUrl($"/reset-password?token={Uri.EscapeDataString(rawToken)}");
        return SafeSendAsync(
            () => _emailService.SendAsync(
                user.Email,
                user.FullName,
                "Restablecer contraseña — VirtualMed",
                EmailTemplates.PasswordReset(user.FullName, url),
                cancellationToken),
            "SendPasswordResetAsync");
    }

    public Task SendPasswordChangedAsync(User user, CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            () => _emailService.SendAsync(
                user.Email,
                user.FullName,
                "Contraseña actualizada — VirtualMed",
                EmailTemplates.PasswordChanged(user.FullName),
                cancellationToken),
            "SendPasswordChangedAsync");

    public Task SendTwoFactorEnabledAsync(User user, CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            () => _emailService.SendAsync(
                user.Email,
                user.FullName,
                "2FA activada — VirtualMed",
                EmailTemplates.TwoFactorEnabled(user.FullName),
                cancellationToken),
            "SendTwoFactorEnabledAsync");

    public Task SendTwoFactorDisabledAsync(User user, CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            () => _emailService.SendAsync(
                user.Email,
                user.FullName,
                "2FA desactivada — VirtualMed",
                EmailTemplates.TwoFactorDisabled(user.FullName),
                cancellationToken),
            "SendTwoFactorDisabledAsync");

    public Task SendDoctorApprovedAsync(User user, CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            () => _emailService.SendAsync(
                user.Email,
                user.FullName,
                "Cuenta médica aprobada — VirtualMed",
                EmailTemplates.DoctorApproved(user.FullName),
                cancellationToken),
            "SendDoctorApprovedAsync");

    public Task SendVitalAlertToPatientAsync(User patientUser, HealthAlert alert, CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            () => _emailService.SendAsync(
                patientUser.Email,
                patientUser.FullName,
                "Nueva alerta de salud — VirtualMed",
                EmailTemplates.VitalAlertPatient(patientUser.FullName, alert.Severity),
                cancellationToken),
            "SendVitalAlertToPatientAsync");

    public Task SendVitalAlertToDoctorAsync(
        User doctorUser,
        string patientFullName,
        HealthAlert alert,
        CancellationToken cancellationToken = default) =>
        SafeSendAsync(
            () => _emailService.SendAsync(
                doctorUser.Email,
                doctorUser.FullName,
                "Alerta clínica de paciente — VirtualMed",
                EmailTemplates.VitalAlertDoctor(doctorUser.FullName, patientFullName, alert.Severity),
                cancellationToken),
            "SendVitalAlertToDoctorAsync");

    private string BuildFrontendUrl(string pathAndQuery) =>
        $"{_settings.FrontendBaseUrl.TrimEnd('/')}{pathAndQuery}";

    private async Task SafeSendAsync(Func<Task> sendAction, string operationName)
    {
        try
        {
            await sendAction();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar correo en {Operation}.", operationName);
        }
    }
}
