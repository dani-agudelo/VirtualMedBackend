using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Application.Interfaces.Services;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Infrastructure.Services;

public class AlertEvaluationService : IAlertEvaluationService
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notification;

    public AlertEvaluationService(IApplicationDbContext context, INotificationService notification)
    {
        _context = context;
        _notification = notification;
    }

    public async Task EvaluateReadingsAsync(
        Guid patientId,
        IReadOnlyList<VitalSignReading> readings,
        CancellationToken cancellationToken = default)
    {
        if (readings.Count == 0)
            return;

        var types = readings.Select(r => r.VitalSignType).Distinct().ToList();
        var thresholds = await _context.Set<AlertThreshold>()
            .AsNoTracking()
            .Where(t => t.PatientId == patientId && t.IsActive && types.Contains(t.VitalSignType))
            .ToListAsync(cancellationToken);

        if (thresholds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var windowStart = now - DuplicateWindow;
        var createdAlerts = new List<HealthAlert>();

        foreach (var reading in readings)
        {
            var threshold = thresholds.FirstOrDefault(t => t.VitalSignType == reading.VitalSignType);
            if (threshold is null)
                continue;

            var isBelow = reading.Value < threshold.MinValue;
            var isAbove = reading.Value > threshold.MaxValue;
            if (!isBelow && !isAbove)
                continue;

            var alertType = $"Threshold:{reading.VitalSignType}:{(isBelow ? "Low" : "High")}";
            var duplicate = await _context.Set<HealthAlert>()
                .AsNoTracking()
                .AnyAsync(
                    a => a.PatientId == patientId
                         && a.AlertType == alertType
                         && a.OccurredAt >= windowStart,
                    cancellationToken);

            if (duplicate)
                continue;

            var severity = threshold.AlertLevel switch
            {
                AlertLevel.High => AlertSeverity.Critical,
                AlertLevel.Medium => AlertSeverity.Warning,
                _ => AlertSeverity.Info
            };

            var direction = isBelow ? "por debajo" : "por encima";
            var alert = new HealthAlert
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                VitalSignReadingId = reading.Id,
                AlertType = alertType,
                Message = $"{reading.VitalSignType} {direction} del umbral ({reading.Value} {reading.Unit}).",
                Severity = severity,
                IsRead = false,
                OccurredAt = now
            };

            _context.Add(alert);
            createdAlerts.Add(alert);
        }

        if (createdAlerts.Count == 0)
            return;

        await _context.SaveChangesAsync(cancellationToken);
        await SendAlertEmailsAsync(patientId, createdAlerts, cancellationToken);
    }

    private async Task SendAlertEmailsAsync(
        Guid patientId,
        IReadOnlyList<HealthAlert> alerts,
        CancellationToken cancellationToken)
    {
        var patient = await _context.Set<Patient>()
            .AsNoTracking()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

        if (patient?.User is null)
            return;

        List<User>? doctorUsers = null;
        if (alerts.Any(a => a.Severity == AlertSeverity.Critical))
        {
            var doctorUserIds = await (
                from appointment in _context.Set<Appointment>()
                where appointment.PatientId == patientId
                join doctor in _context.Set<Doctor>() on appointment.DoctorId equals doctor.Id
                select doctor.UserId
            ).Distinct().ToListAsync(cancellationToken);

            doctorUsers = await _context.Set<User>()
                .AsNoTracking()
                .Where(u => doctorUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }

        foreach (var alert in alerts)
        {
            await _notification.SendVitalAlertToPatientAsync(patient.User, alert, cancellationToken);

            if (alert.Severity == AlertSeverity.Critical && doctorUsers is not null)
            {
                foreach (var doctorUser in doctorUsers)
                {
                    await _notification.SendVitalAlertToDoctorAsync(
                        doctorUser,
                        patient.User.FullName,
                        alert,
                        cancellationToken);
                }
            }

            alert.EmailSentAt = DateTime.UtcNow;
            _context.Update(alert);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
