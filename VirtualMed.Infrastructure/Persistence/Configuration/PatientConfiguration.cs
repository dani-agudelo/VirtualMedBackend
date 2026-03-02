using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Infrastructure.Persistence.Configuration;

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