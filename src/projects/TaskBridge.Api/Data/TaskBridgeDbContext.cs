using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Models;

namespace TaskBridge.Api.Data;

public class TaskBridgeDbContext : DbContext
{
    public TaskBridgeDbContext(DbContextOptions<TaskBridgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.TeamId).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.ConcurrencyToken).IsConcurrencyToken().IsRequired();
            entity.HasIndex(x => x.TeamId);
            entity.HasIndex(x => x.OrganizationId);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.HasIndex(x => new { x.Id, x.OrganizationId }).IsUnique();
        });
    }
}
