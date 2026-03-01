using Microsoft.EntityFrameworkCore;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Entities.Patients;

namespace VirtualMed.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}