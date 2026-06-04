using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Interfaces.Services;

public interface INotificationService
{
    Task NotifyAdminAsync(string message, CancellationToken cancellationToken = default);

    Task SendEmailVerificationAsync(User user, string rawToken, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(User user, string rawToken, CancellationToken cancellationToken = default);

    Task SendPasswordChangedAsync(User user, CancellationToken cancellationToken = default);

    Task SendTwoFactorEnabledAsync(User user, CancellationToken cancellationToken = default);

    Task SendTwoFactorDisabledAsync(User user, CancellationToken cancellationToken = default);

    Task SendDoctorApprovedAsync(User user, CancellationToken cancellationToken = default);

    Task SendVitalAlertToPatientAsync(User patientUser, HealthAlert alert, CancellationToken cancellationToken = default);

    Task SendVitalAlertToDoctorAsync(User doctorUser, string patientFullName, HealthAlert alert, CancellationToken cancellationToken = default);
}
