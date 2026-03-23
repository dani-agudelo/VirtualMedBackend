using Microsoft.EntityFrameworkCore;
using VirtualMed.Domain.Entities;
using VirtualMed.Application.Interfaces;

namespace VirtualMed.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TwoFactorAuth> TwoFactorAuths => Set<TwoFactorAuth>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ClinicalEncounter> ClinicalEncounters => Set<ClinicalEncounter>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PrescriptionMedication> PrescriptionMedications => Set<PrescriptionMedication>();

    IQueryable<T> IApplicationDbContext.Set<T>() => Set<T>();

    void IApplicationDbContext.Add<T>(T entity) => Set<T>().Add(entity);

    void IApplicationDbContext.Update<T>(T entity) => Set<T>().Update(entity);

    void IApplicationDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}