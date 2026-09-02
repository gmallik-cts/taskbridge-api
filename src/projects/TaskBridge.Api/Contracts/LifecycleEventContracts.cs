namespace TaskBridge.Api.Contracts;

public sealed record LifecycleEvent(
    Guid SourceEventId, string EventType, string EntityType, Guid EntityId, Guid ProjectId,
    Guid? MilestoneId, Guid ActorUserId, Guid OrganizationId, DateTime Timestamp,
    string? PreviousStateSnapshot, string? NewStateSnapshot, IReadOnlyCollection<Guid> Recipients,
    string? ActorIpAddress = null);

public interface ILifecycleEventPublisher
{
    Task PublishAsync(LifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default);
}