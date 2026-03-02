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
    public DbSet<User> Users => Set<User>();

    IQueryable<T> IApplicationDbContext.Set<T>() => Set<T>();

    void IApplicationDbContext.Add<T>(T entity) => Set<T>().Add(entity);

    void IApplicationDbContext.Update<T>(T entity) => Set<T>().Update(entity);

    void IApplicationDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
