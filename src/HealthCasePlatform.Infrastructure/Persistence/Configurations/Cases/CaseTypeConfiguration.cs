using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class CaseTypeConfiguration : IEntityTypeConfiguration<CaseType>
{
    public void Configure(EntityTypeBuilder<CaseType> builder)
    {
        builder.ToTable("CaseTypes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(1000);
    }
}
