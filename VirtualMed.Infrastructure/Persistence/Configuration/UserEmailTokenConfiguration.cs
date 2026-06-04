using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VirtualMed.Domain.Entities;

namespace VirtualMed.Infrastructure.Persistence.Configuration;

public class UserEmailTokenConfiguration : IEntityTypeConfiguration<UserEmailToken>
{
    public void Configure(EntityTypeBuilder<UserEmailToken> builder)
    {
        builder.ToTable("user_email_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.TokenType, x.UsedAt });
        builder.HasIndex(x => x.TokenHash).IsUnique();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
