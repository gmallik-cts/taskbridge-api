namespace TaskBridge.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string DefaultOrganizationIdClaimType = "organization_id";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string OrganizationIdClaimType { get; set; } = DefaultOrganizationIdClaimType;
}
