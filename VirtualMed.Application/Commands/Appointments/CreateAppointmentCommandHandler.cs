using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;
namespace VirtualMed.Application.Commands.Appointments;

public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateAppointmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("Authenticated user not found.");
        var role = _currentUserService.Role ?? string.Empty;

        Guid doctorEntityId;

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.DoctorId.HasValue)
                throw new InvalidOperationException("DoctorId is required when creating an appointment as Admin.");

            doctorEntityId = request.DoctorId.Value;
        }
        else if (IsDoctorLikeRole(role))
        {
            var doctor = await _context.Set<Doctor>()
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

            if (doctor is null)
                throw new UnauthorizedAccessException("Only users with a doctor profile can create appointments.");

            doctorEntityId = doctor.Id;

            if (request.DoctorId.HasValue && request.DoctorId.Value != doctorEntityId)
                throw new UnauthorizedAccessException("Cannot assign a different doctor than the authenticated user.");
        }
        else
            throw new UnauthorizedAccessException("You are not allowed to create appointments.");

        var now = DateTime.UtcNow;

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = doctorEntityId,
            ScheduledAt = request.ScheduledAt,
            DurationMinutes = request.DurationMinutes,
            Reason = request.Reason,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Add(appointment);
        await _context.SaveChangesAsync(cancellationToken);
        return appointment.Id;
    }

    private static bool IsDoctorLikeRole(string role) =>
        string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Specialist", StringComparison.OrdinalIgnoreCase);
}
