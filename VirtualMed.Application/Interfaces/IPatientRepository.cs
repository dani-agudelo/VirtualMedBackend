using System;
using System.Collections.Generic;
using System.Text;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Entities.Patients;

namespace VirtualMed.Application.Interfaces
{
    public interface  IPatientRepository
    {
        Task<Patient?> GetByIdAsync(Guid id);
        Task AddAsync(Patient patient);
    }
}