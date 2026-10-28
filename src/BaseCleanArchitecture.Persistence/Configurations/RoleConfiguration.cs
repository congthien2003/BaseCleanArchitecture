using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseCleanArchitecture.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.Name)
            .IsUnique();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasMaxLength(200);

        // Seed data
        builder.HasData(
            new Role
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Administrator role with full access",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = false
            },
            new Role
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Customer",
                Description = "Default customer role",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = false
            },
            new Role
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "Sales",
                Description = "Sales staff role",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = false
            }
        );
    }
}
