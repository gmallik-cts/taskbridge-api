namespace TaskBridge.Notifications.DTOs;

public sealed record NotificationResponse(Guid Id, Guid RecipientUserId, string EventType, Guid ProjectId, Guid? MilestoneId, string Message, bool IsRead, DateTime CreatedAt, DateTime? ReadAt);
public sealed record NotificationPageResponse(IReadOnlyList<NotificationResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages);
public sealed class MarkNotificationReadRequest { public bool IsRead { get; init; } = true; }