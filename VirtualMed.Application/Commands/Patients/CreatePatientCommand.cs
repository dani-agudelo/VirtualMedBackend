using MediatR;
using System;

namespace VirtualMed.Application.Commands.Patients
{
    public record CreatePatientCommand(string Email, string Document) : IRequest<Guid>;
}