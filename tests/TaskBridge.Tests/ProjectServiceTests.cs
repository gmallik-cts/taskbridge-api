using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;
using TaskBridge.Api.Models;
using TaskBridge.Api.Services;

namespace TaskBridge.Tests;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistProject()
    {
        await using var context = await CreateContextAsync();
        var service = new ProjectService(context);

        var project = new Project
        {
            Name = "Website Redesign",
            Description = "Refresh the public site",
            TeamId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Status = ProjectStatus.Draft
        };

        var created = await service.CreateAsync(project);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Website Redesign", created.Name);
        Assert.Equal(ProjectStatus.Draft, created.Status);
        Assert.NotNull(await context.Projects.SingleOrDefaultAsync(p => p.Id == created.Id));
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateExistingProjectStatus()
    {
        await using var context = await CreateContextAsync();
        var service = new ProjectService(context);
        var project = new Project
        {
            Name = "Mobile App",
            TeamId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Status = ProjectStatus.Draft
        };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var updated = await service.UpdateStatusAsync(project.Id, ProjectStatus.Active);

        Assert.NotNull(updated);
        Assert.Equal(ProjectStatus.Active, updated.Status);
        Assert.Equal(ProjectStatus.Active, (await context.Projects.FindAsync(project.Id))!.Status);
    }

    [Fact]
    public async Task GetByTeamAsync_ShouldReturnOnlyProjectsForTheTeam()
    {
        await using var context = await CreateContextAsync();
        var service = new ProjectService(context);
        var teamId = Guid.NewGuid();

        await context.Projects.AddRangeAsync(
            new Project { Name = "Alpha", TeamId = teamId, OrganizationId = Guid.NewGuid(), Status = ProjectStatus.Draft },
            new Project { Name = "Beta", TeamId = teamId, OrganizationId = Guid.NewGuid(), Status = ProjectStatus.Active },
            new Project { Name = "Gamma", TeamId = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Status = ProjectStatus.Draft }
        );
        await context.SaveChangesAsync();

        var projects = await service.GetByTeamAsync(teamId);

        Assert.Equal(2, projects.Count);
        Assert.All(projects, item => Assert.Equal(teamId, item.TeamId));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProject()
    {
        await using var context = await CreateContextAsync();
        var service = new ProjectService(context);
        var project = new Project
        {
            Name = "Archive Cleanup",
            TeamId = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Status = ProjectStatus.Draft
        };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var deleted = await service.DeleteAsync(project.Id);

        Assert.True(deleted);
        Assert.Null(await context.Projects.FindAsync(project.Id));
    }

    private static async Task<TaskBridgeDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TaskBridgeDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TaskBridgeDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
