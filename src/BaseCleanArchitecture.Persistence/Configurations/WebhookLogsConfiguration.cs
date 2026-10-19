using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Persistence.Configurations
{
    public class WebhookLogsConfiguration : IEntityTypeConfiguration<WebhookEvent>
    {
        public void Configure(EntityTypeBuilder<WebhookEvent> builder)
        {
            builder.ToTable("WebhookLogs");

            builder.HasKey(wl => wl.Id);

            builder.HasIndex(wl => new { wl.PartnerCode });
        }
    }
}
