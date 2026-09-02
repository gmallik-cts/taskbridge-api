namespace TaskBridge.Notifications.Models;

public sealed class Notification
{
    private Notification() { }

    public Notification(Guid id, Guid recipientUserId, Guid organizationId, string eventType,
        Guid projectId, Guid? milestoneId, Guid auditEntryId, string message, DateTime createdAt)
    {
        Id = id;
        RecipientUserId = recipientUserId;
        OrganizationId = organizationId;
        EventType = eventType;
        ProjectId = projectId;
        MilestoneId = milestoneId;
        AuditEntryId = auditEntryId;
        Message = message;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string EventType { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public Guid AuditEntryId { get; private set; }
    public string Message { get; private set; } = null!;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public void MarkRead(DateTime readAt)
    {
        IsRead = true;
        ReadAt ??= readAt;
    }
}