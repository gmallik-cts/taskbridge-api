namespace TaskBridge.Api.Security;

public interface ITenantContext
{
    bool IsAuthenticated { get; }
    Guid? OrganizationId { get; }
    bool TryGetOrganizationId(out Guid organizationId);

    bool TryGetActorUserId(out Guid userId) { userId = Guid.Empty; return false; }
    string? ActorIpAddress => null;
}
