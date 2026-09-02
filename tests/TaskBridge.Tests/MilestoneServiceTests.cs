using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskBridge.Api.Contracts;
using TaskBridge.Api.Data;
using TaskBridge.Api.Models;
using TaskBridge.Api.Security;
using TaskBridge.Api.Services;

namespace TaskBridge.Tests;

public sealed class MilestoneServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPublishCreatedEventToDistinctTeamMembers()
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, actorId, actorId, Guid.NewGuid());
        await using var context = project.Context;
        var publisher = new RecordingPublisher();

        var milestone = await CreateService(context, organizationId, actorId, publisher).CreateAsync(new CreateMilestoneRequest
        {
            ProjectId = project.Entity.Id, Name = "Launch", Status = MilestoneStatus.Planned
        });

        var lifecycleEvent = Assert.Single(publisher.Events);
        Assert.Equal("MilestoneCreated", lifecycleEvent.EventType);
        Assert.Null(lifecycleEvent.PreviousStateSnapshot);
        Assert.NotNull(lifecycleEvent.NewStateSnapshot);
        Assert.Equal(2, lifecycleEvent.Recipients.Count);
        Assert.Equal(milestone.Id, lifecycleEvent.MilestoneId);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPublishPreviousAndNewState()
    {
        var organizationId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, Guid.NewGuid());
        var milestone = new Milestone { OrganizationId = organizationId, ProjectId = project.Entity.Id, Name = "Launch", Status = MilestoneStatus.InProgress };
        await project.Context.Milestones.AddAsync(milestone);
        await project.Context.SaveChangesAsync();
        var publisher = new RecordingPublisher();

        await CreateService(project.Context, organizationId, Guid.NewGuid(), publisher).UpdateStatusAsync(milestone.Id, new UpdateMilestoneStatusRequest { ConcurrencyToken = milestone.ConcurrencyToken, Status = MilestoneStatus.Completed });

        var lifecycleEvent = Assert.Single(publisher.Events);
        Assert.Equal("MilestoneStatusUpdated", lifecycleEvent.EventType);
        Assert.Contains("InProgress", lifecycleEvent.PreviousStateSnapshot);
        Assert.Contains("Completed", lifecycleEvent.NewStateSnapshot);
    }

    [Fact]
    public async Task ReopenAsync_ShouldChangeCompletedToInProgressAndPublishAuditData()
    {
        var organizationId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, actorId, actorId, Guid.NewGuid());
        var milestone = new Milestone { OrganizationId = organizationId, ProjectId = project.Entity.Id, Name = "Launch", Status = MilestoneStatus.Completed };
        await project.Context.Milestones.AddAsync(milestone);
        await project.Context.SaveChangesAsync();
        var publisher = new RecordingPublisher();
        var token = milestone.ConcurrencyToken;

        var result = await CreateService(project.Context, organizationId, actorId, publisher).ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = token });

        Assert.Equal(MilestoneStatus.InProgress, result!.Status);
        Assert.NotEqual(token, result.ConcurrencyToken);
        var lifecycleEvent = Assert.Single(publisher.Events);
        Assert.Equal("MILESTONE_REOPENED", lifecycleEvent.EventType);
        Assert.Contains("Completed", lifecycleEvent.PreviousStateSnapshot);
        Assert.Contains("InProgress", lifecycleEvent.NewStateSnapshot);
        Assert.Equal(2, lifecycleEvent.Recipients.Count);
    }

    [Theory]
    [InlineData(MilestoneStatus.Planned)]
    [InlineData(MilestoneStatus.InProgress)]
    public async Task ReopenAsync_ShouldRejectNonCompletedMilestones(MilestoneStatus status)
    {
        var organizationId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, Guid.NewGuid());
        var milestone = new Milestone { OrganizationId = organizationId, ProjectId = project.Entity.Id, Name = "Launch", Status = status };
        await project.Context.Milestones.AddAsync(milestone);
        await project.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => CreateService(project.Context, organizationId, Guid.NewGuid(), new RecordingPublisher()).ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = milestone.ConcurrencyToken }));
    }

    [Fact]
    public async Task ReopenAsync_ShouldCaptureTrustedIpAndRejectStaleTokenAndOtherTenant()
    {
        var organizationId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, Guid.NewGuid());
        var milestone = new Milestone { OrganizationId = organizationId, ProjectId = project.Entity.Id, Name = "Launch", Status = MilestoneStatus.Completed };
        await project.Context.Milestones.AddAsync(milestone);
        await project.Context.SaveChangesAsync();
        var publisher = new RecordingPublisher();
        var service = CreateService(project.Context, organizationId, Guid.NewGuid(), publisher, "2001:db8::1");

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = Guid.NewGuid() }));
        Assert.Null(await CreateService(project.Context, Guid.NewGuid(), Guid.NewGuid(), new RecordingPublisher()).ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = milestone.ConcurrencyToken }));
        await service.ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = milestone.ConcurrencyToken });

        Assert.Equal("2001:db8::1", Assert.Single(publisher.Events).ActorIpAddress);
    }

    [Fact]
    public async Task ReopenAsync_ShouldAllowReopeningAgainAfterCompletion()
    {
        var organizationId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, Guid.NewGuid());
        var milestone = new Milestone { OrganizationId = organizationId, ProjectId = project.Entity.Id, Name = "Launch", Status = MilestoneStatus.Completed };
        await project.Context.Milestones.AddAsync(milestone);
        await project.Context.SaveChangesAsync();
        var publisher = new RecordingPublisher();
        var service = CreateService(project.Context, organizationId, Guid.NewGuid(), publisher);

        var first = await service.ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = milestone.ConcurrencyToken });
        await service.UpdateStatusAsync(milestone.Id, new UpdateMilestoneStatusRequest { ConcurrencyToken = first!.ConcurrencyToken, Status = MilestoneStatus.Completed });
        var second = await service.ReopenAsync(milestone.Id, new ReopenMilestoneRequest { ConcurrencyToken = milestone.ConcurrencyToken });

        Assert.Equal(MilestoneStatus.InProgress, second!.Status);
        Assert.Equal(2, publisher.Events.Count(eventItem => eventItem.EventType == "MILESTONE_REOPENED"));
    }

    [Fact]
    public async Task DeleteAsync_ShouldDispatchBeforeDeletingAndPreserveTenantIsolation()
    {
        var organizationId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, Guid.NewGuid());
        var milestone = new Milestone { OrganizationId = organizationId, ProjectId = project.Entity.Id, Name = "Launch" };
        await project.Context.Milestones.AddAsync(milestone);
        await project.Context.SaveChangesAsync();
        var publisher = new RecordingPublisher();

        Assert.True(await CreateService(project.Context, organizationId, Guid.NewGuid(), publisher).DeleteAsync(milestone.Id));

        Assert.Equal("MilestoneDeleted", Assert.Single(publisher.Events).EventType);
        Assert.Null(await project.Context.Milestones.FindAsync(milestone.Id));
        Assert.False(await CreateService(project.Context, Guid.NewGuid(), Guid.NewGuid(), new RecordingPublisher()).DeleteAsync(milestone.Id));
    }

    [Fact]
    public async Task CreateAsync_ShouldLeaveCommittedStateAndReportIntegrationFailure()
    {
        var organizationId = Guid.NewGuid();
        var project = await SeedProjectAsync(organizationId);
        await AddMembersAsync(project, Guid.NewGuid());
        var publisher = new RecordingPublisher { ShouldFail = true };

        await Assert.ThrowsAsync<IntegrationFailureException>(() => CreateService(project.Context, organizationId, Guid.NewGuid(), publisher).CreateAsync(new CreateMilestoneRequest { ProjectId = project.Entity.Id, Name = "Launch" }));
        Assert.Single(project.Context.Milestones);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectProjectFromAnotherOrganization()
    {
        var project = await SeedProjectAsync(Guid.NewGuid());
        await AddMembersAsync(project, Guid.NewGuid());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CreateService(project.Context, Guid.NewGuid(), Guid.NewGuid(), new RecordingPublisher()).CreateAsync(new CreateMilestoneRequest { ProjectId = project.Entity.Id, Name = "Blocked" }));
    }

    private static MilestoneService CreateService(TaskBridgeDbContext context, Guid organizationId, Guid actorId, RecordingPublisher publisher, string? actorIpAddress = null) =>
        new(context, new TestTenantContext(organizationId, actorId, actorIpAddress), publisher, NullLogger<MilestoneService>.Instance);

    private static async Task<(TaskBridgeDbContext Context, Project Entity)> SeedProjectAsync(Guid organizationId)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var context = new TaskBridgeDbContext(new DbContextOptionsBuilder<TaskBridgeDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var team = new Team { OrganizationId = organizationId };
        var project = new Project { OrganizationId = organizationId, TeamId = team.Id, Name = "Project" };
        await context.Teams.AddAsync(team);
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();
        return (context, project);
    }

    private static async Task AddMembersAsync((TaskBridgeDbContext Context, Project Entity) project, params Guid[] userIds)
    {
        foreach (var userId in userIds) await project.Context.TeamMembers.AddAsync(new TeamMember { TeamId = project.Entity.TeamId, UserId = userId });
        await project.Context.SaveChangesAsync();
    }

    private sealed class RecordingPublisher : ILifecycleEventPublisher
    {
        public List<LifecycleEvent> Events { get; } = [];
        public bool ShouldFail { get; init; }
        public Task PublishAsync(LifecycleEvent lifecycleEvent, CancellationToken cancellationToken = default)
        {
            if (ShouldFail) throw new HttpRequestException("unavailable");
            Events.Add(lifecycleEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class TestTenantContext(Guid organizationId, Guid actorId, string? actorIpAddress = null) : ITenantContext
    {
        public bool IsAuthenticated => true;
        public Guid? OrganizationId => organizationId;
        public bool TryGetOrganizationId(out Guid value) { value = organizationId; return true; }
        public bool TryGetActorUserId(out Guid value) { value = actorId; return true; }
        public string? ActorIpAddress => actorIpAddress;
    }
}