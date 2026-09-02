using Microsoft.EntityFrameworkCore;
using TaskBridge.Notifications.Models;

namespace TaskBridge.Notifications.Data;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(64);
            entity.Property(x => x.EntityType).IsRequired().HasMaxLength(32);
            entity.Property(x => x.PreviousStateSnapshot).HasColumnType("jsonb");
            entity.Property(x => x.NewStateSnapshot).HasColumnType("jsonb");
            entity.HasIndex(x => x.SourceEventId).IsUnique();
            entity.HasIndex(x => new { x.ActorOrganizationId, x.ProjectId, x.Timestamp, x.EventType });
        });
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(500);
            entity.HasIndex(x => new { x.OrganizationId, x.ProjectId });
            entity.HasIndex(x => new { x.OrganizationId, x.RecipientUserId, x.CreatedAt });
            entity.HasIndex(x => new { x.AuditEntryId, x.RecipientUserId }).IsUnique();
        });
    }
}