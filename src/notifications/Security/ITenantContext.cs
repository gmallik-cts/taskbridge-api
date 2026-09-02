namespace TaskBridge.Notifications.Security;

public interface ITenantContext
{
    bool IsAuthenticated { get; }
    bool TryGetOrganizationId(out Guid organizationId);
    bool TryGetUserId(out Guid userId);
    bool TryGetActorUserId(out Guid userId);
}