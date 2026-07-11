using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.CaseId).IsRequired();
        builder.Property(a => a.Action).HasConversion<int>().IsRequired();
        builder.Property(a => a.Actor).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Detail).HasMaxLength(2000);
        builder.Property(a => a.OccurredAt).IsRequired();

        builder.HasIndex(a => a.CaseId);

        builder.HasOne<RegulatoryCase>()
            .WithMany()
            .HasForeignKey(a => a.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
