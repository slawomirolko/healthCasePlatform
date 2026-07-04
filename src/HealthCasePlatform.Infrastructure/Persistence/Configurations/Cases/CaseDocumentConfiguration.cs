using HealthCasePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCasePlatform.Infrastructure.Persistence.Configurations.Cases;

public sealed class CaseDocumentConfiguration : IEntityTypeConfiguration<CaseDocument>
{
    public void Configure(EntityTypeBuilder<CaseDocument> builder)
    {
        builder.ToTable("CaseDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.CaseId).IsRequired();
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(300);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(d => d.BlobReference).IsRequired().HasMaxLength(500);
        builder.Property(d => d.UploadedBy).IsRequired().HasMaxLength(100);
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.HasIndex(d => d.CaseId);
    }
}
