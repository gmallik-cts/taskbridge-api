using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TaskBridge.Api.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddTaskBridgeAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.Key))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) || string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        }

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ITenantContext>(provider =>
            new TenantContext(
                provider.GetRequiredService<IHttpContextAccessor>(),
                jwtOptions.OrganizationIdClaimType));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.MapInboundClaims = false;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("TenantAccess", policy =>
                policy.RequireAuthenticatedUser()
                    .RequireClaim(jwtOptions.OrganizationIdClaimType)
                    .RequireAssertion(context =>
                    {
                        var claim = context.User.FindFirst(jwtOptions.OrganizationIdClaimType);
                        return claim is not null && Guid.TryParse(claim.Value, out _);
                    }));
        });

        return services;
    }
}
