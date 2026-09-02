using System.Security.Claims;

namespace TaskBridge.Notifications.Security;

public sealed class TenantContext(IHttpContextAccessor accessor, string organizationClaim) : ITenantContext
{
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool TryGetOrganizationId(out Guid organizationId) => TryGetGuid(organizationClaim, out organizationId);

    public bool TryGetUserId(out Guid userId)
    {
        return TryGetGuid(ClaimTypes.NameIdentifier, out userId) || TryGetGuid("sub", out userId);
    }

    public bool TryGetActorUserId(out Guid userId)
    {
        return TryGetGuid("actor_user_id", out userId) || TryGetUserId(out userId);
    }

    private bool TryGetGuid(string claimType, out Guid value)
    {
        value = Guid.Empty;
        var claim = accessor.HttpContext?.User.FindFirst(claimType);
        return IsAuthenticated && claim is not null && Guid.TryParse(claim.Value, out value) && value != Guid.Empty;
    }
}