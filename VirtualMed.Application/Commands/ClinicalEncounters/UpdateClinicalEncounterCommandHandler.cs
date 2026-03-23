using MediatR;
using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Common.Exceptions;
using VirtualMed.Application.Exceptions;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Commands.ClinicalEncounters;

public class UpdateClinicalEncounterCommandHandler : IRequestHandler<UpdateClinicalEncounterCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateClinicalEncounterCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateClinicalEncounterCommand request, CancellationToken cancellationToken)
    {
        _ = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user not found.");

        var role = _currentUserService.Role ?? string.Empty;
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Solo un administrador puede actualizar encuentros clínicos.");

        var encounter = await _context.Set<ClinicalEncounter>()
            .Include(e => e.Diagnoses)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (encounter is null)
            throw new NotFoundException("Encuentro clínico", request.Id);

        if (request.StartAt.HasValue)
            encounter.StartAt = request.StartAt.Value;

        if (request.EndAt.HasValue)
            encounter.EndAt = request.EndAt;

        if (request.ChiefComplaint is not null)
            encounter.ChiefComplaint = request.ChiefComplaint;

        if (request.CurrentCondition is not null)
            encounter.CurrentCondition = request.CurrentCondition;

        if (request.PhysicalExam is not null)
            encounter.PhysicalExam = request.PhysicalExam;

        if (request.Assessment is not null)
            encounter.Assessment = request.Assessment;

        if (request.Plan is not null)
            encounter.Plan = request.Plan;

        if (request.Notes is not null)
            encounter.Notes = request.Notes;

        if (request.RecordingUrl is not null)
            encounter.RecordingUrl = request.RecordingUrl;

        if (request.IsLocked.HasValue)
            encounter.IsLocked = request.IsLocked.Value;

        if (request.Diagnoses is not null)
        {
            foreach (var existing in encounter.Diagnoses.ToList())
                _context.Remove(existing);
            encounter.Diagnoses.Clear();

            var now = DateTime.UtcNow;
            foreach (var d in request.Diagnoses)
            {
                encounter.Diagnoses.Add(new Diagnosis
                {
                    Id = Guid.NewGuid(),
                    EncounterId = encounter.Id,
                    Icd10Code = d.Icd10Code.Trim().ToUpperInvariant(),
                    Description = d.Description.Trim(),
                    Type = d.Type,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (encounter.EndAt.HasValue && encounter.EndAt.Value < encounter.StartAt)
            throw new BusinessRuleException("EndAt no puede ser anterior a StartAt.");

        encounter.UpdatedAt = DateTime.UtcNow;
        _context.Update(encounter);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
