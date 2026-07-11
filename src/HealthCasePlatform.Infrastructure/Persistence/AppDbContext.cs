using HealthCasePlatform.Domain.Cases;
using HealthCasePlatform.Domain.Cases.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HealthCasePlatform.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public DbSet<RegulatoryCase> RegulatoryCases => Set<RegulatoryCase>();
    public DbSet<CaseStatusHistory> CaseStatusHistories => Set<CaseStatusHistory>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<CaseType> CaseTypes => Set<CaseType>();
    public DbSet<CaseDocument> CaseDocuments => Set<CaseDocument>();
    public DbSet<CaseTask> CaseTasks => Set<CaseTask>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Decision> Decisions => Set<Decision>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
