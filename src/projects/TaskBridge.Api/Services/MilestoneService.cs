using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Contracts;
using TaskBridge.Api.Data;
using TaskBridge.Api.Models;
using TaskBridge.Api.Security;

namespace TaskBridge.Api.Services;

public interface IMilestoneService
{
    Task<MilestoneResponse> CreateAsync(CreateMilestoneRequest request, CancellationToken cancellationToken = default);
    Task<MilestoneResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MilestoneResponse?> UpdateStatusAsync(Guid id, UpdateMilestoneStatusRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class MilestoneService(
    TaskBridgeDbContext dbContext,
    ITenantContext tenantContext,
    ILifecycleEventPublisher eventPublisher,
    ILogger<MilestoneService> logger) : IMilestoneService
{
    public async Task<MilestoneResponse> CreateAsync(CreateMilestoneRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var actorUserId = GetActorUserId();
        ValidateRequest(request);
        var project = await GetProjectAsync(request.ProjectId, organizationId, cancellationToken) ?? throw new ResourceNotFoundException("Project was not found.");

        var milestone = new Milestone
        {
            OrganizationId = organizationId, ProjectId = project.Id, Name = request.Name.Trim(),
            Description = request.Description, Status = request.Status, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };
        await dbContext.Milestones.AddAsync(milestone, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await PublishAsync(await CreateEventAsync("MilestoneCreated", milestone, project, actorUserId, null, Snapshot(milestone), organizationId, cancellationToken), cancellationToken);
        return MilestoneResponse.FromEntity(milestone);
    }

    public async Task<MilestoneResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var milestone = await dbContext.Milestones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);
        return milestone is null ? null : MilestoneResponse.FromEntity(milestone);
    }

    public async Task<MilestoneResponse?> UpdateStatusAsync(Guid id, UpdateMilestoneStatusRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var actorUserId = GetActorUserId();
        ValidateRequest(request);
        var milestone = await dbContext.Milestones.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);
        if (milestone is null) return null;
        if (milestone.ConcurrencyToken != request.ConcurrencyToken) throw new ConcurrencyConflictException("The milestone was changed by another request.");

        var project = await GetProjectAsync(milestone.ProjectId, organizationId, cancellationToken) ?? throw new ResourceNotFoundException("Project was not found.");
        var previous = Snapshot(milestone);
        milestone.Status = request.Status;
        milestone.ConcurrencyToken = Guid.NewGuid();
        milestone.UpdatedAtUtc = DateTime.UtcNow;
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("The milestone was changed by another request."); }

        await PublishAsync(await CreateEventAsync("MilestoneStatusUpdated", milestone, project, actorUserId, previous, Snapshot(milestone), organizationId, cancellationToken), cancellationToken);
        return MilestoneResponse.FromEntity(milestone);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var actorUserId = GetActorUserId();
        var milestone = await dbContext.Milestones.FirstOrDefaultAsync(x => x.Id == id && x.OrganizationId == organizationId, cancellationToken);
        if (milestone is null) return false;
        var project = await GetProjectAsync(milestone.ProjectId, organizationId, cancellationToken) ?? throw new ResourceNotFoundException("Project was not found.");

        // Publish before hard deletion so recipient lookup and the prior snapshot are still available.
        await PublishAsync(await CreateEventAsync("MilestoneDeleted", milestone, project, actorUserId, Snapshot(milestone), null, organizationId, cancellationToken), cancellationToken);
        dbContext.Milestones.Remove(milestone);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Project?> GetProjectAsync(Guid projectId, Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId && x.OrganizationId == organizationId, cancellationToken);

    private async Task PublishAsync(LifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        try { await eventPublisher.PublishAsync(lifecycleEvent, cancellationToken); }
        catch (Exception exception) when (exception is not IntegrationFailureException)
        {
            logger.LogError(exception, "Notification integration failed for lifecycle event {SourceEventId}.", lifecycleEvent.SourceEventId);
            throw new IntegrationFailureException("The lifecycle event could not be recorded.");
        }
    }

    private async Task<LifecycleEvent> CreateEventAsync(string eventType, Milestone milestone, Project project, Guid actorUserId, string? previous, string? next, Guid organizationId, CancellationToken cancellationToken)
    {
        var recipients = await dbContext.TeamMembers
            .Where(x => x.TeamId == project.TeamId && x.Team.OrganizationId == organizationId)
            .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
        if (recipients.Count == 0) throw new ArgumentException("The project team has no notification recipients.");
        return new LifecycleEvent(Guid.NewGuid(), eventType, "Milestone", milestone.Id, project.Id, milestone.Id, actorUserId, organizationId, DateTime.UtcNow, previous, next, recipients);
    }

    private Guid GetOrganizationId() => tenantContext.TryGetOrganizationId(out var id) ? id : throw new AuthenticationRequiredException("An authenticated organization is required.");
    private Guid GetActorUserId() => tenantContext.TryGetActorUserId(out var id) ? id : throw new AuthenticationRequiredException("An authenticated actor is required.");

    private static string Snapshot(Milestone x) => JsonSerializer.Serialize(new { x.Id, x.ProjectId, x.Name, x.Description, Status = x.Status.ToString(), x.CreatedAtUtc, x.UpdatedAtUtc });
    private static void ValidateRequest(CreateMilestoneRequest request)
    {
        if (request is null || request.ProjectId == Guid.Empty) throw new ArgumentException("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200) throw new ArgumentException("Milestone name is required and must be 200 characters or fewer.");
        if (request.Description?.Length > 2000) throw new ArgumentException("Milestone description must be 2000 characters or fewer.");
        ValidateStatus(request.Status);
    }
    private static void ValidateRequest(UpdateMilestoneStatusRequest request)
    {
        if (request is null || request.ConcurrencyToken == Guid.Empty) throw new ArgumentException("A concurrency token is required.");
        ValidateStatus(request.Status);
    }
    private static void ValidateStatus(MilestoneStatus status) { if (!Enum.IsDefined(status)) throw new ArgumentException("Milestone status is invalid."); }
}