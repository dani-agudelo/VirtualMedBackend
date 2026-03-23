using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Commands.Appointments;

public class UpdateAppointmentCommandHandler : IRequestHandler<UpdateAppointmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateAppointmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("Authenticated user not found.");
        var role = _currentUserService.Role ?? string.Empty;

        var appointment = await _context.Set<Appointment>()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (appointment is null)
            throw new InvalidOperationException("Appointment not found.");

        await EnsureCanModifyAppointmentAsync(appointment, userId, role, cancellationToken);

        if (request.Status.HasValue)
            appointment.Status = request.Status.Value;

        if (request.ScheduledAt.HasValue)
            appointment.ScheduledAt = request.ScheduledAt.Value;

        if (request.DurationMinutes.HasValue)
            appointment.DurationMinutes = request.DurationMinutes.Value;

        if (request.Reason != null)
            appointment.Reason = request.Reason;

        appointment.UpdatedAt = DateTime.UtcNow;
        _context.Update(appointment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCanModifyAppointmentAsync(
        Appointment appointment,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return;

        if (IsDoctorLikeRole(role))
        {
            var doctor = await _context.Set<Doctor>()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

            if (doctor is null || appointment.DoctorId != doctor.Id)
                throw new UnauthorizedAccessException("You can only update your own appointments.");

            return;
        }

        throw new UnauthorizedAccessException("You are not allowed to update appointments.");
    }

    private static bool IsDoctorLikeRole(string role) =>
        string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Specialist", StringComparison.OrdinalIgnoreCase);
}
