using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskBridge.Notifications.Data;
using TaskBridge.Notifications.Middleware;
using TaskBridge.Notifications.Repositories;
using TaskBridge.Notifications.Security;
using TaskBridge.Notifications.Services;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience)) throw new InvalidOperationException("JWT configuration is incomplete.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext>(sp => new TenantContext(sp.GetRequiredService<IHttpContextAccessor>(), jwt.OrganizationIdClaimType));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), ClockSkew = TimeSpan.FromMinutes(1) };
    options.MapInboundClaims = false;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantAccess", policy => policy.RequireAuthenticatedUser().RequireClaim(jwt.OrganizationIdClaimType).RequireAssertion(context => Guid.TryParse(context.User.FindFirst(jwt.OrganizationIdClaimType)?.Value, out var id) && id != Guid.Empty));
    options.AddPolicy("AuditIngestion", policy => policy.RequireAuthenticatedUser().RequireClaim(jwt.OrganizationIdClaimType).RequireClaim("service", "ProjectApi"));
});
builder.Services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }