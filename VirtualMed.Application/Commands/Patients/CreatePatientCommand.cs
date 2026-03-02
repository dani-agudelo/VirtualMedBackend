using MediatR;

namespace VirtualMed.Application.Commands.Patients;

public record CreatePatientCommand(
    Guid UserId,
    string Document,
    DateTime DateOfBirth,
    string Gender,
    string BloodType,
    string? Allergies) : IRequest<Guid>;