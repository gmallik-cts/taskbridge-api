using TaskBridge.Notifications.DTOs;
using TaskBridge.Notifications.Repositories;
using TaskBridge.Notifications.Security;

namespace TaskBridge.Notifications.Services;

public interface INotificationService
{
    Task<NotificationPageResponse> GetAsync(Guid userId, bool? isRead, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<NotificationResponse> MarkReadAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class NotificationService(INotificationRepository notifications, ITenantContext tenantContext) : INotificationService
{
    public async Task<NotificationPageResponse> GetAsync(Guid userId, bool? isRead, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (!tenantContext.TryGetOrganizationId(out var organizationId) || !tenantContext.TryGetUserId(out var callerId)) throw new UnauthorizedAccessException("A valid authenticated user is required.");
        if (userId == Guid.Empty) throw new ArgumentException("User ID must be a non-empty GUID.");
        if (userId != callerId) throw new ForbiddenOperationException("Users may only read their own notifications.");
        AuditService.ValidatePaging(pageNumber, pageSize);
        var result = await notifications.GetForRecipientAsync(organizationId, userId, isRead, pageNumber, pageSize, cancellationToken);
        return new NotificationPageResponse(result.Items.Select(x => new NotificationResponse(x.Id, x.RecipientUserId, x.EventType, x.ProjectId, x.MilestoneId, x.Message, x.IsRead, x.CreatedAt, x.ReadAt)).ToList(), pageNumber, pageSize, result.TotalCount, result.TotalCount == 0 ? 0 : (result.TotalCount + pageSize - 1) / pageSize);
    }

    public async Task<NotificationResponse> MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!tenantContext.TryGetOrganizationId(out var organizationId) || !tenantContext.TryGetUserId(out var userId)) throw new UnauthorizedAccessException("A valid authenticated user is required.");
        if (id == Guid.Empty) throw new ArgumentException("Notification ID must be a non-empty GUID.");
        var notification = await notifications.GetForUpdateAsync(organizationId, userId, id, cancellationToken) ?? throw new ForbiddenOperationException("Notification is not owned by the authenticated user.");
        notification.MarkRead(DateTime.UtcNow);
        await notifications.SaveChangesAsync(cancellationToken);
        return new NotificationResponse(notification.Id, notification.RecipientUserId, notification.EventType, notification.ProjectId, notification.MilestoneId, notification.Message, notification.IsRead, notification.CreatedAt, notification.ReadAt);
    }
}