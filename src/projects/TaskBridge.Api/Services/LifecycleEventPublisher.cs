using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using TaskBridge.Api.Contracts;

namespace TaskBridge.Api.Services;

public sealed class LifecycleEventPublisher(HttpClient client, IConfiguration configuration, ILogger<LifecycleEventPublisher> logger) : ILifecycleEventPublisher
{
    public async Task PublishAsync(LifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "audit") { Content = JsonContent.Create(lifecycleEvent) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateServiceToken(lifecycleEvent));

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Notification integration rejected lifecycle event {SourceEventId} with status {StatusCode}.", lifecycleEvent.SourceEventId, (int)response.StatusCode);
            throw new IntegrationFailureException("The lifecycle event could not be recorded.");
        }
    }

    private string CreateServiceToken(LifecycleEvent lifecycleEvent)
    {
        var key = configuration["NotificationIntegration:SigningKey"];
        if (string.IsNullOrWhiteSpace(key)) throw new IntegrationFailureException("Notification integration signing is not configured.");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("service", "ProjectApi"),
                new Claim(configuration["Jwt:OrganizationIdClaimType"] ?? "organization_id", lifecycleEvent.OrganizationId.ToString()),
                new Claim("actor_user_id", lifecycleEvent.ActorUserId.ToString())]),
            Issuer = configuration["NotificationIntegration:Issuer"] ?? "TaskBridge",
            Audience = configuration["NotificationIntegration:Audience"] ?? "TaskBridge.Notifications",
            Expires = DateTime.UtcNow.AddMinutes(2),
            SigningCredentials = credentials
        };
        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }
}