using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaseCleanArchitecture.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs", "audit");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.AuditType).HasConversion<string>().HasMaxLength(20);
            builder.Property(a => a.HttpMethod).HasMaxLength(10);
            builder.Property(a => a.Path).HasMaxLength(2048);
            builder.Property(a => a.ErrorMessage).HasMaxLength(4000);
            builder.Property(a => a.Username).HasMaxLength(256);
            builder.Property(a => a.RequestBody).HasColumnType("text");
            builder.Property(a => a.ResponseBody).HasColumnType("text");
            builder.Property(a => a.Changes).HasColumnType("text");

            builder.HasIndex(a => a.CreatedAt);
            builder.HasIndex(a => new { a.AuditType, a.StatusCode, a.CreatedAt });
        }
    }
}
