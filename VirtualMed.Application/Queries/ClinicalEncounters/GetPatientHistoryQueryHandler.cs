using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Queries.ClinicalEncounters;

public class GetPatientHistoryQueryHandler : IRequestHandler<GetPatientHistoryQuery, IReadOnlyCollection<ClinicalEncounterListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPatientHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyCollection<ClinicalEncounterListItemDto>> Handle(GetPatientHistoryQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("Authenticated user not found.");
        var role = _currentUserService.Role ?? string.Empty;

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            // Sin restricción adicional.
        }
        else if (string.Equals(role, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            var selfPatientId = await _context.Set<Patient>()
                .Where(x => x.UserId == userId)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!selfPatientId.HasValue || selfPatientId.Value != request.PatientId)
                throw new ForbiddenException("No tiene permiso para acceder al historial de este paciente.");
        }
        else if (IsDoctorLikeRole(role))
        {
            var doctorId = await _context.Set<Doctor>()
                .Where(x => x.UserId == userId)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!doctorId.HasValue)
                throw new ForbiddenException("Solo los usuarios con perfil médico pueden acceder a este historial.");

            var hasAttendedPatient = await _context.Set<ClinicalEncounter>()
                .AnyAsync(x => x.Appointment.PatientId == request.PatientId && x.Appointment.DoctorId == doctorId.Value, cancellationToken);

            if (!hasAttendedPatient)
                throw new ForbiddenException("Solo puede acceder al historial de pacientes que haya atendido.");
        }
        else
            throw new ForbiddenException("No tiene permiso para acceder al historial del paciente.");

        var query = _context.Set<ClinicalEncounter>()
            .AsNoTracking()
            .Where(x => x.Appointment.PatientId == request.PatientId);

        if (request.From.HasValue)
            query = query.Where(x => x.StartAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(x => x.StartAt <= request.To.Value);

        var data = await query
            .OrderByDescending(x => x.StartAt)
            .Select(x => new ClinicalEncounterListItemDto
            {
                Id = x.Id,
                AppointmentId = x.AppointmentId,
                PatientId = x.Appointment.PatientId,
                DoctorId = x.Appointment.DoctorId,
                StartAt = x.StartAt,
                EndAt = x.EndAt,
                ChiefComplaint = x.ChiefComplaint,
                Diagnoses = x.Diagnoses
                    .Select(d => new DiagnosisDto
                    {
                        Id = d.Id,
                        Icd10Code = d.Icd10Code,
                        Description = d.Description,
                        Type = d.Type
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return data;
    }

    private static bool IsDoctorLikeRole(string role) =>
        string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Specialist", StringComparison.OrdinalIgnoreCase);
}

