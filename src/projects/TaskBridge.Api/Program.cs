using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Contracts;
using TaskBridge.Api.Data;
using TaskBridge.Api.Middleware;
using TaskBridge.Api.Security;
using TaskBridge.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddTaskBridgeAuthentication(builder.Configuration);

builder.Services.AddDbContext<TaskBridgeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddHttpClient<ILifecycleEventPublisher, LifecycleEventPublisher>()
    .ConfigureHttpClient((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["NotificationIntegration:BaseUrl"] ?? "https://localhost:5000/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
