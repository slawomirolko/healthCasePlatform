using HealthCasePlatform.Domain.Cases.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.Type).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.OccurredAtUtc).IsRequired();
        builder.Property(o => o.ProcessedAtUtc);
        builder.Property(o => o.Attempts);
        builder.Property(o => o.LastError).HasMaxLength(2000);

        builder.HasIndex(o => new { o.ProcessedAtUtc, o.OccurredAtUtc })
            .HasDatabaseName("IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc");
    }
}
