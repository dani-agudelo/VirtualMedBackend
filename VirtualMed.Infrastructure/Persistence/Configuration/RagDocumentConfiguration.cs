using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Infrastructure.Persistence.Configuration;

public class RagDocumentConfiguration : IEntityTypeConfiguration<RagDocument>
{
    public void Configure(EntityTypeBuilder<RagDocument> builder)
    {
        builder.ToTable("rag_documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.NormalizedFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.NormalizedFileName).IsUnique();
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
