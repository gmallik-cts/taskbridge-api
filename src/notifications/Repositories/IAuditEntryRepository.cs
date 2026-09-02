using TaskBridge.Notifications.Models;

namespace TaskBridge.Notifications.Repositories;

public interface IAuditEntryRepository
{
    Task<AuditEntry?> GetBySourceEventIdAsync(Guid sourceEventId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<AuditEntry> Items, int TotalCount)> GetProjectAsync(Guid organizationId, Guid projectId, DateTime? from, DateTime? to, string? eventType, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);
}