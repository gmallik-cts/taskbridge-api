using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskBridge.Api.Security;

namespace TaskBridge.Tests;

public class TenantContextTests
{
    [Fact]
    public void TryGetOrganizationId_ShouldResolveGuidFromAuthenticatedClaim()
    {
        var organizationId = Guid.Parse("f5bf8c88-1214-4d53-9af0-6139a8d7b4b0");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim("organization_id", organizationId.ToString()) },
                "Bearer"));

        var tenantContext = new TenantContext(new HttpContextAccessor { HttpContext = httpContext }, "organization_id");

        var resolved = tenantContext.TryGetOrganizationId(out var actualOrganizationId);

        Assert.True(resolved);
        Assert.Equal(organizationId, actualOrganizationId);
    }

    [Fact]
    public void TryGetOrganizationId_ShouldReturnFalseWhenClaimIsMissingOrInvalid()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim("organization_id", "not-a-guid") },
                "Bearer"));

        var tenantContext = new TenantContext(new HttpContextAccessor { HttpContext = httpContext }, "organization_id");

        var resolved = tenantContext.TryGetOrganizationId(out var actualOrganizationId);

        Assert.False(resolved);
        Assert.Equal(Guid.Empty, actualOrganizationId);
    }
}
