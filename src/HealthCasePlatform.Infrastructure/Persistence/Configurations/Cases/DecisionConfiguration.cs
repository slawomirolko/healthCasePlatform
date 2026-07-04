using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("Decisions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.CaseId).IsRequired();
        builder.Property(d => d.DecisionText).IsRequired().HasMaxLength(2000);
        builder.Property(d => d.DecidedBy).IsRequired().HasMaxLength(100);
        builder.Property(d => d.DecidedAt).IsRequired();

        builder.HasIndex(d => d.CaseId);
    }
}
