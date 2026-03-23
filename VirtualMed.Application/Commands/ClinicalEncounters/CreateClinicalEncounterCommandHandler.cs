using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Commands.ClinicalEncounters;

public class CreateClinicalEncounterCommandHandler : IRequestHandler<CreateClinicalEncounterCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateClinicalEncounterCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateClinicalEncounterCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("Authenticated user not found.");

        var doctor = await _context.Set<Doctor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (doctor is null)
            throw new UnauthorizedAccessException("Only doctors can create clinical encounters.");

        var appointment = await _context.Set<Appointment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
            throw new InvalidOperationException("Appointment not found.");

        if (appointment.DoctorId != doctor.Id)
            throw new UnauthorizedAccessException("Doctor can only create encounters for assigned appointments.");

        var now = DateTime.UtcNow;
        var encounter = new ClinicalEncounter
        {
            Id = Guid.NewGuid(),
            AppointmentId = request.AppointmentId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            ChiefComplaint = request.ChiefComplaint,
            CurrentCondition = request.CurrentCondition,
            PhysicalExam = request.PhysicalExam,
            Assessment = request.Assessment,
            Plan = request.Plan,
            Notes = request.Notes,
            RecordingUrl = request.RecordingUrl,
            IsLocked = false,
            CreatedAt = now,
            UpdatedAt = now,
            Diagnoses = request.Diagnoses.Select(d => new Diagnosis
            {
                Id = Guid.NewGuid(),
                Icd10Code = d.Icd10Code.Trim().ToUpperInvariant(),
                Description = d.Description.Trim(),
                Type = d.Type,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList()
        };

        _context.Add(encounter);
        await _context.SaveChangesAsync(cancellationToken);
        return encounter.Id;
    }
}

