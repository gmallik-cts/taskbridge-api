using System.Text.Json;
using TaskBridge.Notifications.DTOs;
using TaskBridge.Notifications.Models;
using TaskBridge.Notifications.Repositories;
using TaskBridge.Notifications.Security;

namespace TaskBridge.Notifications.Services;

public interface IAuditService
{
    Task<AuditCreateResponse> CreateAsync(CreateAuditRequest request, CancellationToken cancellationToken);
    Task<AuditPageResponse> GetProjectAsync(Guid projectId, DateTime? from, DateTime? to, string? eventType, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public sealed class AuditService(IAuditEntryRepository audits, INotificationRepository notifications, ITenantContext tenantContext) : IAuditService
{
    private static readonly HashSet<string> EventTypes = ["ProjectCreated", "ProjectUpdated", "ProjectDeleted", "MilestoneCreated", "MilestoneStatusUpdated", "MilestoneDeleted"];
    private static readonly HashSet<string> EntityTypes = ["Project", "Milestone"];

    public async Task<AuditCreateResponse> CreateAsync(CreateAuditRequest request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TryGetOrganizationId(out var organizationId) || !tenantContext.TryGetActorUserId(out var actorUserId)) throw new UnauthorizedAccessException("A valid authenticated actor is required.");
        Validate(request, organizationId, actorUserId);
        var existing = await audits.GetBySourceEventIdAsync(request.SourceEventId, cancellationToken);
        if (existing is not null)
        {
            if (Matches(existing, request, organizationId)) return new AuditCreateResponse(ToResponse(existing), 0, true);
            throw new ConflictOperationException("SourceEventId has already been used for a different event.");
        }
        var now = DateTime.UtcNow;
        var entry = new AuditEntry(Guid.NewGuid(), request.SourceEventId, request.EventType!, request.EntityType!, request.EntityId, request.ProjectId, request.MilestoneId, request.ActorUserId, organizationId, request.PreviousStateSnapshot, request.NewStateSnapshot, request.Timestamp, now);
        await audits.AddAsync(entry, cancellationToken);
        var recipients = request.Recipients!.Distinct().Select(userId => new Notification(Guid.NewGuid(), userId, organizationId, request.EventType!, request.ProjectId, request.MilestoneId, entry.Id, BuildMessage(request), now)).ToList();
        await notifications.AddRangeAsync(recipients, cancellationToken);
        await notifications.SaveChangesAsync(cancellationToken);
        return new AuditCreateResponse(ToResponse(entry), recipients.Count);
    }

    public async Task<AuditPageResponse> GetProjectAsync(Guid projectId, DateTime? from, DateTime? to, string? eventType, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        if (!tenantContext.TryGetOrganizationId(out var organizationId)) throw new UnauthorizedAccessException("A valid tenant is required.");
        ValidatePaging(pageNumber, pageSize);
        if (projectId == Guid.Empty || (from.HasValue && from.Value.Kind != DateTimeKind.Utc) || (to.HasValue && to.Value.Kind != DateTimeKind.Utc)) throw new ArgumentException("Project ID and dates must be valid UTC values.");
        if (from.HasValue && to.HasValue && from >= to) throw new ArgumentException("The from date must precede the to date.");
        if (eventType is not null && !EventTypes.Contains(eventType)) throw new ArgumentException("Unsupported event type.");
        var result = await audits.GetProjectAsync(organizationId, projectId, from, to, eventType, pageNumber, pageSize, cancellationToken);
        return new AuditPageResponse(result.Items.Select(ToResponse).ToList(), pageNumber, pageSize, result.TotalCount, Pages(result.TotalCount, pageSize));
    }

    private static void Validate(CreateAuditRequest request, Guid organizationId, Guid actorUserId)
    {
        if (request.SourceEventId == Guid.Empty || request.EntityId == Guid.Empty || request.ProjectId == Guid.Empty || request.ActorUserId == Guid.Empty) throw new ArgumentException("Required identifiers must be non-empty GUIDs.");
        if (request.OrganizationId != organizationId) throw new ForbiddenOperationException("Organization does not match the authenticated tenant.");
        if (request.ActorUserId != actorUserId) throw new ForbiddenOperationException("Actor does not match the authenticated identity.");
        if (request.EventType is null || !EventTypes.Contains(request.EventType) || request.EntityType is null || !EntityTypes.Contains(request.EntityType)) throw new ArgumentException("Unsupported event or entity type.");
        if ((request.EntityType == "Project" && request.MilestoneId.HasValue) || (request.EntityType == "Milestone" && !request.MilestoneId.HasValue) || (request.EntityType == "Milestone" && request.EntityId != request.MilestoneId)) throw new ArgumentException("Invalid event/entity combination.");
        if (request.Timestamp.Kind != DateTimeKind.Utc || request.Timestamp > DateTime.UtcNow.AddMinutes(5) || request.Timestamp < DateTime.UtcNow.AddDays(-30)) throw new ArgumentException("Timestamp must be a recent UTC value.");
        ValidateSnapshot(request.PreviousStateSnapshot, request.NewStateSnapshot, request.EventType);
        if (request.Recipients is null || request.Recipients.Count == 0 || request.Recipients.Any(x => x == Guid.Empty)) throw new ArgumentException("Recipients must contain non-empty GUIDs.");
    }

    private static void ValidateSnapshot(string? previous, string? next, string eventType)
    {
        if (previous?.Length > 10000 || next?.Length > 10000) throw new ArgumentException("Snapshots are too large.");
        try
        {
            if (previous is not null && JsonDocument.Parse(previous).RootElement.ValueKind != JsonValueKind.Object || next is not null && JsonDocument.Parse(next).RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("Snapshots must be JSON objects.");
        }
        catch (JsonException) { throw new ArgumentException("Snapshots must be valid JSON objects."); }
        var creation = eventType.EndsWith("Created", StringComparison.Ordinal);
        var deletion = eventType.EndsWith("Deleted", StringComparison.Ordinal);
        if ((creation && (previous is not null || next is null)) || (deletion && (previous is null || next is not null)) || (!creation && !deletion && (previous is null || next is null))) throw new ArgumentException("Snapshots do not match the event type.");
    }

    private static bool Matches(AuditEntry entry, CreateAuditRequest request, Guid organizationId) => entry.ActorOrganizationId == organizationId && entry.EventType == request.EventType && entry.EntityId == request.EntityId && entry.ProjectId == request.ProjectId && entry.ActorUserId == request.ActorUserId && entry.Timestamp == request.Timestamp && entry.PreviousStateSnapshot == request.PreviousStateSnapshot && entry.NewStateSnapshot == request.NewStateSnapshot;
    private static string BuildMessage(CreateAuditRequest request) => $"{request.EventType} for project {request.ProjectId}";
    private static AuditResponse ToResponse(AuditEntry x) => new(x.Id, x.EventType, x.EntityType, x.EntityId, x.ActorUserId, x.ActorOrganizationId, x.PreviousStateSnapshot, x.NewStateSnapshot, x.Timestamp);
    internal static void ValidatePaging(int pageNumber, int pageSize) { if (pageNumber < 1 || pageSize < 1 || pageSize > 100) throw new ArgumentException("Invalid pagination."); }
    private static int Pages(int count, int size) => count == 0 ? 0 : (count + size - 1) / size;
}