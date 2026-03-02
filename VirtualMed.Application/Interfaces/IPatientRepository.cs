using VirtualMed.Domain.Entities;

namespace VirtualMed.Application.Interfaces
{
    public interface  IPatientRepository
    {
        Task<Patient?> GetByIdAsync(Guid id);
        Task AddAsync(Patient patient);
    }
}