using BaseCleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace BaseCleanArchitecture.Persistence.Configurations
{
    public class WebhookEventsConfiguration : IEntityTypeConfiguration<WebhookEvent>
    {
        public void Configure(EntityTypeBuilder<WebhookEvent> builder)
        {
            builder.ToTable("WebhookEvents", "webhooks");

            builder.HasKey(wl => wl.Id);

            builder.HasIndex(wl => new { wl.PartnerCode });
        }
    }
}
