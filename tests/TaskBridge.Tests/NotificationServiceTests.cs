using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskBridge.Notifications.Data;
using TaskBridge.Notifications.DTOs;
using TaskBridge.Notifications.Repositories;
using TaskBridge.Notifications.Security;
using TaskBridge.Notifications.Services;

namespace TaskBridge.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateImmutableAuditAndDistinctNotifications()
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var service = CreateAuditService(context, organizationId, actorId);
        var request = CreateRequest(organizationId, actorId, "ProjectCreated", DateTime.UtcNow.AddMinutes(-1));
        request.Recipients = new[] { actorId, actorId };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.Equal(1, result.NotificationCount);
        Assert.Single(context.AuditEntries);
        Assert.Single(context.Notifications);
        Assert.DoesNotContain(typeof(IAuditEntryRepository).GetMethods(), method => method.Name.Contains("Update", StringComparison.OrdinalIgnoreCase) || method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetProjectAsync_ShouldFilterByDateAndEventTypeAndTenant()
    {
        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var repository = new AuditEntryRepository(context);
        var otherTenant = Guid.NewGuid();
        await repository.AddAsync(new TaskBridge.Notifications.Models.AuditEntry(Guid.NewGuid(), Guid.NewGuid(), "ProjectCreated", "Project", Guid.NewGuid(), projectId, null, Guid.NewGuid(), organizationId, null, "{}", DateTime.Parse("2026-09-01T00:00:00Z").ToUniversalTime(), DateTime.UtcNow), CancellationToken.None);
        await repository.AddAsync(new TaskBridge.Notifications.Models.AuditEntry(Guid.NewGuid(), Guid.NewGuid(), "ProjectUpdated", "Project", Guid.NewGuid(), projectId, null, Guid.NewGuid(), organizationId, "{}", "{}", DateTime.Parse("2026-09-02T00:00:00Z").ToUniversalTime(), DateTime.UtcNow), CancellationToken.None);
        await repository.AddAsync(new TaskBridge.Notifications.Models.AuditEntry(Guid.NewGuid(), Guid.NewGuid(), "ProjectCreated", "Project", Guid.NewGuid(), projectId, null, Guid.NewGuid(), otherTenant, null, "{}", DateTime.Parse("2026-09-01T12:00:00Z").ToUniversalTime(), DateTime.UtcNow), CancellationToken.None);
        await context.SaveChangesAsync();

        var page = await CreateAuditService(context, organizationId).GetProjectAsync(projectId, DateTime.Parse("2026-09-01T00:00:00Z").ToUniversalTime(), DateTime.Parse("2026-09-02T00:00:00Z").ToUniversalTime(), "ProjectCreated", 1, 20, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal("ProjectCreated", item.EventType);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task NotificationService_ShouldRejectOtherUserAndTenant()
    {
        var organizationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var notification = new TaskBridge.Notifications.Models.Notification(Guid.NewGuid(), ownerId, organizationId, "ProjectCreated", Guid.NewGuid(), null, Guid.NewGuid(), "message", DateTime.UtcNow);
        await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();
        var service = CreateNotificationService(context, organizationId, Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.GetAsync(ownerId, null, 1, 20, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.MarkReadAsync(notification.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidRequestAndOrganizationSpoofing()
    {
        var organizationId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var service = CreateAuditService(context, organizationId);
        var invalid = CreateRequest(organizationId, Guid.NewGuid(), "ProjectCreated", DateTime.UtcNow);
        invalid.OrganizationId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenOperationException>(() => service.CreateAsync(invalid, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetProjectAsync(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(-1), null, 1, 20, CancellationToken.None));
    }

    private static CreateAuditRequest CreateRequest(Guid organizationId, Guid actorId, string eventType, DateTime timestamp) => new()
    {
        SourceEventId = Guid.NewGuid(), EventType = eventType, EntityType = "Project", EntityId = Guid.NewGuid(), ProjectId = Guid.NewGuid(), ActorUserId = actorId,
        OrganizationId = organizationId, Timestamp = timestamp, NewStateSnapshot = "{}", Recipients = new[] { actorId }
    };

    private static AuditService CreateAuditService(NotificationDbContext context, Guid organizationId, Guid? userId = null) => new(new AuditEntryRepository(context), new NotificationRepository(context), new TestTenantContext(organizationId, userId ?? Guid.NewGuid()));
    private static NotificationService CreateNotificationService(NotificationDbContext context, Guid organizationId, Guid userId) => new(new NotificationRepository(context), new TestTenantContext(organizationId, userId));

    private static async Task<NotificationDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NotificationDbContext>().UseSqlite(connection).Options;
        var context = new NotificationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class TestTenantContext(Guid organizationId, Guid userId) : ITenantContext
    {
        public bool IsAuthenticated => true;
        public bool TryGetOrganizationId(out Guid value) { value = organizationId; return true; }
        public bool TryGetUserId(out Guid value) { value = userId; return true; }
        public bool TryGetActorUserId(out Guid value) { value = userId; return true; }
    }
}