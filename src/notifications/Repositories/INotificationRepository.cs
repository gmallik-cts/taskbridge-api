using TaskBridge.Notifications.Models;

namespace TaskBridge.Notifications.Repositories;

public interface INotificationRepository
{
    Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetForRecipientAsync(Guid organizationId, Guid userId, bool? isRead, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<Notification?> GetForUpdateAsync(Guid organizationId, Guid userId, Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}