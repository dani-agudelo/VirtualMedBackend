using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualMed.Domain.Entities;
using VirtualMed.Domain.Entities.Patients;

namespace VirtualMed.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.DateOfBirth)
            .IsRequired();

        builder.Property(p => p.Document)
            .HasMaxLength(20)
            .IsRequired();
    }
}