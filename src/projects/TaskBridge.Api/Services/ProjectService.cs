using Microsoft.EntityFrameworkCore;
using TaskBridge.Api.Data;
using TaskBridge.Api.Models;
using TaskBridge.Api.Security;

namespace TaskBridge.Api.Services;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectResponse?> UpdateStatusAsync(Guid id, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default);
    Task<PagedProjectResponse> GetByTeamAsync(Guid teamId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ProjectService : IProjectService
{
    public const int MaxPageSize = 100;
    private readonly TaskBridgeDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public ProjectService(TaskBridgeDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        ValidateRequest(request);
        await ValidateTeamAsync(request.TeamId, organizationId, cancellationToken);

        var project = new Project
        {
            OrganizationId = organizationId,
            TeamId = request.TeamId,
            Name = request.Name.Trim(),
            Description = request.Description,
            Status = request.Status
        };

        project.CreatedAtUtc = DateTime.UtcNow;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.Projects.AddAsync(project, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProjectResponse.FromEntity(project);
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);
        return project is null ? null : ProjectResponse.FromEntity(project);
    }

    public async Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        ValidateRequest(request);

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (project.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new ConcurrencyConflictException("The project was changed by another request.");
        }

        await ValidateTeamAsync(request.TeamId, organizationId, cancellationToken);

        project.TeamId = request.TeamId;
        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.Status = request.Status;
        project.ConcurrencyToken = Guid.NewGuid();
        project.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The project was changed by another request.");
        }
        return ProjectResponse.FromEntity(project);
    }

    public async Task<ProjectResponse?> UpdateStatusAsync(Guid id, UpdateProjectStatusRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        ValidateStatusRequest(request);
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (project.ConcurrencyToken != request.ConcurrencyToken)
        {
            throw new ConcurrencyConflictException("The project was changed by another request.");
        }

        project.Status = request.Status;
        project.ConcurrencyToken = Guid.NewGuid();
        project.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The project was changed by another request.");
        }
        return ProjectResponse.FromEntity(project);
    }

    public async Task<PagedProjectResponse> GetByTeamAsync(Guid teamId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        ValidatePagination(pageNumber, pageSize);
        var query = _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.TeamId == teamId && p.OrganizationId == organizationId)
            .OrderBy(p => p.Name);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ProjectResponse.FromEntity(p))
            .ToListAsync(cancellationToken);

        return new PagedProjectResponse
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);
        if (project is null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Guid GetOrganizationId()
    {
        if (!_tenantContext.TryGetOrganizationId(out var organizationId))
        {
            throw new AuthenticationRequiredException("An authenticated organization is required.");
        }

        return organizationId;
    }

    private static void ValidateRequest(CreateProjectRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.TeamId == Guid.Empty)
        {
            throw new ArgumentException("TeamId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
        {
            throw new ArgumentException("Project name is required and must be 200 characters or fewer.", nameof(request));
        }

        if (request.Description?.Length > 2000)
        {
            throw new ArgumentException("Project description must be 2000 characters or fewer.", nameof(request));
        }

        ValidateStatus(request.Status);
    }

    private static void ValidateRequest(UpdateProjectRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.ConcurrencyToken == Guid.Empty)
        {
            throw new ArgumentException("A concurrency token is required.", nameof(request));
        }

        if (request.TeamId == Guid.Empty)
        {
            throw new ArgumentException("TeamId is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
        {
            throw new ArgumentException("Project name is required and must be 200 characters or fewer.", nameof(request));
        }

        if (request.Description?.Length > 2000)
        {
            throw new ArgumentException("Project description must be 2000 characters or fewer.", nameof(request));
        }

        ValidateStatus(request.Status);
    }

    private static void ValidateStatus(ProjectStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentException("Project status is invalid.", nameof(status));
        }
    }

    private async Task ValidateTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken)
    {
        var team = await _dbContext.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            throw new ArgumentException("TeamId must reference an existing team.", nameof(teamId));
        }

        if (team.OrganizationId != organizationId)
        {
            throw new ForbiddenOperationException("The team does not belong to the authenticated organization.");
        }
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be at least 1.", nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentException($"Page size must be between 1 and {MaxPageSize}.", nameof(pageSize));
        }
    }

    private static void ValidateStatusRequest(UpdateProjectStatusRequest request)
    {
        if (request is null || request.ConcurrencyToken == Guid.Empty)
        {
            throw new ArgumentException("A concurrency token is required.", nameof(request));
        }

        ValidateStatus(request.Status);
    }
}
