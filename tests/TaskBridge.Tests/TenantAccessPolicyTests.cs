using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskBridge.Api.Security;

namespace TaskBridge.Tests;

public class TenantAccessPolicyTests
{
    [Fact]
    public async Task TenantAccess_ShouldAllowAuthenticatedUserWithValidOrganizationIdClaim()
    {
        var organizationId = Guid.NewGuid();
        var authorizationService = BuildAuthorizationService("tenant_id");
        var principal = CreateAuthenticatedPrincipal("tenant_id", organizationId.ToString());

        var result = await authorizationService.AuthorizeAsync(principal, "TenantAccess");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task TenantAccess_ShouldDenyAuthenticatedUserWhenOrganizationIdClaimIsMissing()
    {
        var authorizationService = BuildAuthorizationService("tenant_id");
        var principal = CreateAuthenticatedPrincipal("other_claim", Guid.NewGuid().ToString());

        var result = await authorizationService.AuthorizeAsync(principal, "TenantAccess");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task TenantAccess_ShouldDenyAuthenticatedUserWhenOrganizationIdClaimIsInvalid()
    {
        var authorizationService = BuildAuthorizationService("tenant_id");
        var principal = CreateAuthenticatedPrincipal("tenant_id", "not-a-guid");

        var result = await authorizationService.AuthorizeAsync(principal, "TenantAccess");

        Assert.False(result.Succeeded);
    }

    private static IAuthorizationService BuildAuthorizationService(string organizationIdClaimType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JwtOptions.SectionName}:Key"] = "test-signing-key-test-signing-key",
                [$"{JwtOptions.SectionName}:Issuer"] = "test-issuer",
                [$"{JwtOptions.SectionName}:Audience"] = "test-audience",
                [$"{JwtOptions.SectionName}:OrganizationIdClaimType"] = organizationIdClaimType
            })
            .Build();

        var services = new ServiceCollection();
        services.AddTaskBridgeAuthentication(configuration);
        services.AddLogging();

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(string claimType, string claimValue)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(claimType, claimValue) },
                "Bearer"));
    }
}