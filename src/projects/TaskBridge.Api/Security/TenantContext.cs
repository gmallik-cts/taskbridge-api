using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace TaskBridge.Api.Security;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _organizationIdClaimType;

    public TenantContext(IHttpContextAccessor httpContextAccessor, string? organizationIdClaimType = null)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _organizationIdClaimType = string.IsNullOrWhiteSpace(organizationIdClaimType)
            ? JwtOptions.DefaultOrganizationIdClaimType
            : organizationIdClaimType;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? OrganizationId
    {
        get
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            return TryGetOrganizationId(out var organizationId) ? organizationId : null;
        }
    }

    public bool TryGetOrganizationId(out Guid organizationId)
    {
        organizationId = Guid.Empty;

        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        var claim = user.Claims.FirstOrDefault(c => string.Equals(c.Type, _organizationIdClaimType, StringComparison.Ordinal));
        if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
        {
            return false;
        }

        if (!Guid.TryParse(claim.Value, out organizationId))
        {
            return false;
        }

        return organizationId != Guid.Empty;
    }

    public bool TryGetActorUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        var claim = user.Claims.FirstOrDefault(c => c.Type == "actor_user_id" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier || c.Type == "sub");
        return claim is not null && Guid.TryParse(claim.Value, out userId) && userId != Guid.Empty;
    }
}
