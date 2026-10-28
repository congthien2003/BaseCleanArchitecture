using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseCleanArchitecture.Persistence.Configurations;

public class PasswordResetOtpConfiguration : IEntityTypeConfiguration<PasswordResetOtp>
{
    public void Configure(EntityTypeBuilder<PasswordResetOtp> builder)
    {
        builder.ToTable("PasswordResetOtps", "identity");

        builder.HasKey(o => o.Id);

        builder.HasIndex(o => o.UserId);

        builder.Property(o => o.CodeHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
