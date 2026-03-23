using MediatR;

namespace VirtualMed.Application.Queries.ClinicalEncounters;

public record GetPatientHistoryQuery(
    Guid PatientId,
    DateTime? From,
    DateTime? To) : IRequest<IReadOnlyCollection<ClinicalEncounterListItemDto>>;

