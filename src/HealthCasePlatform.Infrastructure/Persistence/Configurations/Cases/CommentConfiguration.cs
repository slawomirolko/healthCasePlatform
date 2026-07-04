using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.CaseId).IsRequired();
        builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.Author).IsRequired().HasMaxLength(100);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.CaseId);
    }
}
