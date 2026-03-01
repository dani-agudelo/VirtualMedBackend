using Microsoft.EntityFrameworkCore;
using VirtualMed.Application.Interfaces;
using VirtualMed.Domain.Entities.Patients;
using VirtualMed.Infrastructure.Persistence;

namespace VirtualMed.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly ApplicationDbContext _context;

    public PatientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(Guid id)
    {
        return await _context.Patients.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Patient patient)
    {
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();
    }
}