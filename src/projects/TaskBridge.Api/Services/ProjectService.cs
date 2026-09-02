using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;
using TaskBridge.Api.Models;

namespace TaskBridge.Api.Services;

public interface IProjectService
{
    Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default);
    Task<Project?> UpdateStatusAsync(Guid id, ProjectStatus status, CancellationToken cancellationToken = default);
    Task<List<Project>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ProjectService : IProjectService
{
    private readonly TaskBridgeDbContext _dbContext;

    public ProjectService(TaskBridgeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        if (project is null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (string.IsNullOrWhiteSpace(project.Name))
        {
            throw new ArgumentException("Project name is required.", nameof(project));
        }

        project.CreatedAtUtc = DateTime.UtcNow;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.Projects.AddAsync(project, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return project;
    }

    public async Task<Project?> UpdateStatusAsync(Guid id, ProjectStatus status, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        project.Status = status;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<List<Project>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Where(p => p.TeamId == teamId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
