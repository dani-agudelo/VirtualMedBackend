using MediatR;
using VirtualMed.Domain.Enums;

namespace VirtualMed.Application.Commands.Patients;

public record CreatePatientCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    IdentificationType? IdentificationType,
    string Document,
    DateTime DateOfBirth,
    string Gender,
    string? PhoneNumber,
    bool AcceptPrivacy,
    bool AuthorizeData) : IRequest<Guid>;