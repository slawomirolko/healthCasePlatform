using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class CaseStatusHistoryConfiguration : IEntityTypeConfiguration<CaseStatusHistory>
{
    public void Configure(EntityTypeBuilder<CaseStatusHistory> builder)
    {
        builder.ToTable("CaseStatusHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.CaseId).IsRequired();
        builder.Property(h => h.FromStatus).HasConversion<int>().IsRequired();
        builder.Property(h => h.ToStatus).HasConversion<int>().IsRequired();
        builder.Property(h => h.TransitionedAt).IsRequired();

        builder.HasIndex(h => h.CaseId);
    }
}
