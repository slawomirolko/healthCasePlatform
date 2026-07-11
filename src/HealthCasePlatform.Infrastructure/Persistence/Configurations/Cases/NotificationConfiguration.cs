using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.CaseId).IsRequired();
        builder.Property(n => n.Type).HasConversion<int>().IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();

        builder.HasIndex(n => n.CaseId);
        builder.HasIndex(n => new { n.CaseId, n.Type })
            .IsUnique()
            .HasDatabaseName("IX_Notifications_CaseId_Type");

        builder.HasOne<RegulatoryCase>()
            .WithMany()
            .HasForeignKey(n => n.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
