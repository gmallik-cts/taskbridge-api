using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;
using TaskBridge.Api.Models;
using TaskBridge.Api.Security;
using TaskBridge.Api.Services;

namespace TaskBridge.Tests;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldUseOrganizationFromTenantContext()
    {
        var organizationId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        await AddTeamAsync(context, teamId, organizationId);
        var service = CreateService(context, organizationId);

        var created = await service.CreateAsync(new CreateProjectRequest
        {
            Name = "Website Redesign",
            Description = "Refresh the public site",
            TeamId = teamId,
            Status = ProjectStatus.Draft
        });

        Assert.Equal(organizationId, created.OrganizationId);
        Assert.Equal(organizationId, (await context.Projects.SingleAsync()).OrganizationId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldNotReturnProjectFromAnotherOrganization()
    {
        await using var context = await CreateContextAsync();
        var project = new Project { Name = "Other tenant", TeamId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var service = CreateService(context, Guid.NewGuid());

        Assert.Null(await service.GetByIdAsync(project.Id));
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotUpdateProjectFromAnotherOrganization()
    {
        await using var context = await CreateContextAsync();
        var project = new Project { Name = "Original", TeamId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();
        var originalName = project.Name;

        var service = CreateService(context, Guid.NewGuid());
        var result = await service.UpdateAsync(project.Id, new UpdateProjectRequest
        {
            Name = "Attempted takeover",
            TeamId = project.TeamId,
            ConcurrencyToken = project.ConcurrencyToken,
            Status = ProjectStatus.Active
        });

        Assert.Null(result);
        Assert.Equal(originalName, (await context.Projects.FindAsync(project.Id))!.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotDeleteProjectFromAnotherOrganization()
    {
        await using var context = await CreateContextAsync();
        var project = new Project { Name = "Protected", TeamId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var service = CreateService(context, Guid.NewGuid());

        Assert.False(await service.DeleteAsync(project.Id));
        Assert.NotNull(await context.Projects.FindAsync(project.Id));
    }

    [Fact]
    public async Task GetByTeamAsync_ShouldReturnOnlyProjectsForTenantAndTeam()
    {
        var organizationId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        await context.Projects.AddRangeAsync(
            new Project { Name = "Alpha", TeamId = teamId, OrganizationId = organizationId },
            new Project { Name = "Other tenant", TeamId = teamId, OrganizationId = Guid.NewGuid() },
            new Project { Name = "Other team", TeamId = Guid.NewGuid(), OrganizationId = organizationId });
        await context.SaveChangesAsync();

        var page = await CreateService(context, organizationId).GetByTeamAsync(teamId);

        var project = Assert.Single(page.Items);
        Assert.Equal("Alpha", project.Name);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectInvalidRequest()
    {
        await using var context = await CreateContextAsync();
        var service = CreateService(context, Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(new CreateProjectRequest
        {
            Name = " ",
            TeamId = Guid.Empty
        }));
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotAllowOrganizationIdToBeChanged()
    {
        var organizationId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var project = new Project { Name = "Original", TeamId = Guid.NewGuid(), OrganizationId = organizationId };
        await AddTeamAsync(context, project.TeamId, organizationId);
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var updated = await CreateService(context, organizationId).UpdateAsync(project.Id, new UpdateProjectRequest
        {
            Name = "Updated",
            TeamId = project.TeamId,
            ConcurrencyToken = project.ConcurrencyToken,
            Status = ProjectStatus.Active
        });

        Assert.NotNull(updated);
        Assert.Equal(organizationId, updated.OrganizationId);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectTeamFromAnotherOrganization()
    {
        var organizationId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var team = new Team { OrganizationId = Guid.NewGuid() };
        await context.Teams.AddAsync(team);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            CreateService(context, organizationId).CreateAsync(new CreateProjectRequest
            {
                Name = "Cross tenant",
                TeamId = team.Id
            }));

        Assert.Contains("does not belong", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectTeamFromAnotherOrganization()
    {
        var organizationId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        var ownTeam = new Team { OrganizationId = organizationId };
        var otherTeam = new Team { OrganizationId = Guid.NewGuid() };
        var project = new Project { Name = "Project", TeamId = ownTeam.Id, OrganizationId = organizationId };
        await context.Teams.AddRangeAsync(ownTeam, otherTeam);
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            CreateService(context, organizationId).UpdateAsync(project.Id, new UpdateProjectRequest
            {
                Name = project.Name,
                TeamId = otherTeam.Id,
                Status = project.Status,
                ConcurrencyToken = project.ConcurrencyToken
            }));
    }

    [Fact]
    public async Task GetByTeamAsync_ShouldValidatePaginationAndReturnMetadata()
    {
        var organizationId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        await AddTeamAsync(context, teamId, organizationId);
        await context.Projects.AddRangeAsync(
            new Project { Name = "Alpha", TeamId = teamId, OrganizationId = organizationId },
            new Project { Name = "Bravo", TeamId = teamId, OrganizationId = organizationId },
            new Project { Name = "Charlie", TeamId = teamId, OrganizationId = organizationId });
        await context.SaveChangesAsync();

        var service = CreateService(context, organizationId);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByTeamAsync(teamId, 0, 10));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetByTeamAsync(teamId, 1, ProjectService.MaxPageSize + 1));

        var page = await service.GetByTeamAsync(teamId, 2, 2);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("Charlie", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectStaleConcurrencyToken()
    {
        var organizationId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        await using var context = await CreateContextAsync();
        await AddTeamAsync(context, teamId, organizationId);
        var project = new Project { Name = "Original", TeamId = teamId, OrganizationId = organizationId };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();
        var staleToken = project.ConcurrencyToken;

        await CreateService(context, organizationId).UpdateAsync(project.Id, new UpdateProjectRequest
        {
            Name = "First update",
            TeamId = teamId,
            Status = ProjectStatus.Active,
            ConcurrencyToken = staleToken
        });

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => CreateService(context, organizationId).UpdateAsync(project.Id, new UpdateProjectRequest
        {
            Name = "Stale update",
            TeamId = teamId,
            Status = ProjectStatus.Completed,
            ConcurrencyToken = staleToken
        }));
    }

    private static async Task AddTeamAsync(TaskBridgeDbContext context, Guid teamId, Guid organizationId)
    {
        await context.Teams.AddAsync(new Team { Id = teamId, OrganizationId = organizationId });
        await context.SaveChangesAsync();
    }

    private static ProjectService CreateService(TaskBridgeDbContext context, Guid organizationId)
    {
        return new ProjectService(context, new TestTenantContext(organizationId));
    }

    private static async Task<TaskBridgeDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<TaskBridgeDbContext>().UseSqlite(connection).Options;
        var context = new TaskBridgeDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private sealed class TestTenantContext(Guid organizationId) : ITenantContext
    {
        public bool IsAuthenticated => true;
        public Guid? OrganizationId => organizationId;

        public bool TryGetOrganizationId(out Guid resolvedOrganizationId)
        {
            resolvedOrganizationId = organizationId;
            return organizationId != Guid.Empty;
        }
    }
}
