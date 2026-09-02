namespace TaskBridge.Notifications.DTOs;

public sealed class CreateAuditRequest
{
    public Guid SourceEventId { get; set; }
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public Guid EntityId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? PreviousStateSnapshot { get; set; }
    public string? NewStateSnapshot { get; set; }
    public IReadOnlyCollection<Guid>? Recipients { get; set; }
}

public sealed record AuditResponse(Guid Id, string EventType, string EntityType, Guid EntityId, Guid ActorUserId, Guid ActorOrganizationId, string? PreviousStateSnapshot, string? NewStateSnapshot, DateTime Timestamp);
public sealed record AuditCreateResponse(AuditResponse Audit, int NotificationCount, bool IsDuplicate = false);
public sealed record AuditPageResponse(IReadOnlyList<AuditResponse> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages);