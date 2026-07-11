using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class RegulatoryCaseConfiguration : IEntityTypeConfiguration<RegulatoryCase>
{
    public void Configure(EntityTypeBuilder<RegulatoryCase> builder)
    {
        builder.ToTable("RegulatoryCases");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.Property(c => c.CaseTypeId).IsRequired();
        builder.Property(c => c.Status).HasConversion<int>().IsRequired();
        builder.Property(c => c.Priority).HasConversion<int>().IsRequired();
        builder.Property(c => c.Country).IsRequired().HasMaxLength(2);
        builder.Property(c => c.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt);
        builder.Property(c => c.AssignedScientificReviewerId).HasMaxLength(100);
        builder.Property(c => c.AssignedLegalReviewerId).HasMaxLength(100);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.Priority);
        builder.HasIndex(c => c.Country);
        builder.HasIndex(c => new { c.CreatedAt, c.Id });

        builder.HasMany(c => c.Documents)
            .WithOne()
            .HasForeignKey(d => d.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Documents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Tasks)
            .WithOne()
            .HasForeignKey(t => t.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Tasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Comments)
            .WithOne()
            .HasForeignKey(cm => cm.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Comments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.Decisions)
            .WithOne()
            .HasForeignKey(d => d.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Decisions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(c => c.History)
            .WithOne()
            .HasForeignKey(h => h.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
