using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Infrastructure.Persistence.Configuration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAt)
            .HasColumnName("OccurredAt")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .IsRequired();

        builder.Property(x => x.TableName)
            .HasColumnName("TableName")
            .IsRequired();

        builder.Property(x => x.Operation)
            .HasColumnName("Operation")
            .IsRequired();

        builder.Property(x => x.RowPk)
            .HasColumnName("RowPk")
            .IsRequired();

        builder.Property(x => x.OldData)
            .HasColumnName("OldData")
            .HasColumnType("jsonb");

        builder.Property(x => x.NewData)
            .HasColumnName("NewData")
            .HasColumnType("jsonb");

        builder.Property(x => x.DbUser)
            .HasColumnName("DbUser")
            .IsRequired();

        builder.Property(x => x.AppUserId)
            .HasColumnName("AppUserId");
    }
}

