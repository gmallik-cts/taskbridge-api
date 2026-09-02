namespace TaskBridge.Api.Security;

public interface ITenantContext
{
    bool IsAuthenticated { get; }
    Guid? OrganizationId { get; }
    bool TryGetOrganizationId(out Guid organizationId);
}
