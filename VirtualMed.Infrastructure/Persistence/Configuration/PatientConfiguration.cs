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
            .IsRequired();

        builder.HasIndex(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId);

        builder.Property(p => p.DateOfBirth)
            .IsRequired();

        builder.Property(p => p.Document)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Gender)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.BloodType)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Allergies)
            .HasMaxLength(2000);
    }
}