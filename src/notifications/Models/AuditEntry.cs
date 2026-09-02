namespace TaskBridge.Notifications.Models;

public sealed class AuditEntry
{
    private AuditEntry() { }

    public AuditEntry(Guid id, Guid sourceEventId, string eventType, string entityType, Guid entityId,
        Guid projectId, Guid? milestoneId, Guid actorUserId, Guid actorOrganizationId,
        string? previousStateSnapshot, string? newStateSnapshot, DateTime timestamp, DateTime createdAt)
    {
        Id = id;
        SourceEventId = sourceEventId;
        EventType = eventType;
        EntityType = entityType;
        EntityId = entityId;
        ProjectId = projectId;
        MilestoneId = milestoneId;
        ActorUserId = actorUserId;
        ActorOrganizationId = actorOrganizationId;
        PreviousStateSnapshot = previousStateSnapshot;
        NewStateSnapshot = newStateSnapshot;
        Timestamp = timestamp;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid SourceEventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? MilestoneId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public Guid ActorOrganizationId { get; private set; }
    public string? PreviousStateSnapshot { get; private set; }
    public string? NewStateSnapshot { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime CreatedAt { get; private set; }
}