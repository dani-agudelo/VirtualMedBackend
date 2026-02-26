using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualMed.Application.Commands.Patients
{
    public class CreatePatientCommandHandler
        : IRequestHandler<CreatePatientCommand, Guid>
    {
        public Task<Guid> Handle(
            CreatePatientCommand request,
            CancellationToken cancellationToken)
        {
            // Aquí iría la lógica real (usar DbContext, repositorios, etc.)
            var newId = Guid.NewGuid();
            return Task.FromResult(newId);
        }
    }
}