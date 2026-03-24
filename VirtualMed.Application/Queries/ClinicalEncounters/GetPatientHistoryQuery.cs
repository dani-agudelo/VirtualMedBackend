using MediatR;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Queries.ClinicalEncounters;

public record GetPatientHistoryQuery(
    Guid PatientId,
    DateTime? From,
    DateTime? To,
    EncounterType? EncounterType) : IRequest<IReadOnlyCollection<ClinicalEncounterListItemDto>>;

