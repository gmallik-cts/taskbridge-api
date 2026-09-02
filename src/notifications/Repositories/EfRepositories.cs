using Microsoft.EntityFrameworkCore;
using TaskBridge.Notifications.Data;
using TaskBridge.Notifications.Models;

namespace TaskBridge.Notifications.Repositories;

public sealed class AuditEntryRepository(NotificationDbContext context) : IAuditEntryRepository
{
    public Task<AuditEntry?> GetBySourceEventIdAsync(Guid sourceEventId, CancellationToken cancellationToken) => context.AuditEntries.AsNoTracking().SingleOrDefaultAsync(x => x.SourceEventId == sourceEventId, cancellationToken);
    public async Task<(IReadOnlyList<AuditEntry> Items, int TotalCount)> GetProjectAsync(Guid organizationId, Guid projectId, DateTime? from, DateTime? to, string? eventType, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.AuditEntries.AsNoTracking().Where(x => x.ActorOrganizationId == organizationId && x.ProjectId == projectId);
        if (from.HasValue) query = query.Where(x => x.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Timestamp < to.Value);
        if (eventType is not null) query = query.Where(x => x.EventType == eventType);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.Timestamp).ThenByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken) => await context.AuditEntries.AddAsync(entry, cancellationToken);
}

public sealed class NotificationRepository(NotificationDbContext context) : INotificationRepository
{
    public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken) => await context.Notifications.AddRangeAsync(notifications, cancellationToken);
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetForRecipientAsync(Guid organizationId, Guid userId, bool? isRead, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Notifications.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.RecipientUserId == userId);
        if (isRead.HasValue) query = query.Where(x => x.IsRead == isRead.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
    public Task<Notification?> GetForUpdateAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken) => context.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId && x.RecipientUserId == userId, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}