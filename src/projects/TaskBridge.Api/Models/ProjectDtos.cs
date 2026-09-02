using System.ComponentModel.DataAnnotations;

namespace TaskBridge.Api.Models;

public class CreateProjectRequest
{
    [Required]
    public Guid TeamId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
}

public class UpdateProjectRequest
{
    [Required]
    public Guid ConcurrencyToken { get; set; }

    [Required]
    public Guid TeamId { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
}

public class UpdateProjectStatusRequest
{
    [Required]
    public Guid ConcurrencyToken { get; set; }

    public ProjectStatus Status { get; set; }
}

public class ProjectResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid TeamId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public ProjectStatus Status { get; init; }
    public Guid ConcurrencyToken { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }

    public static ProjectResponse FromEntity(Project project) => new()
    {
        Id = project.Id,
        OrganizationId = project.OrganizationId,
        TeamId = project.TeamId,
        Name = project.Name,
        Description = project.Description,
        Status = project.Status,
        ConcurrencyToken = project.ConcurrencyToken,
        CreatedAtUtc = project.CreatedAtUtc,
        UpdatedAtUtc = project.UpdatedAtUtc
    };
}

public class PagedProjectResponse
{
    public IReadOnlyList<ProjectResponse> Items { get; init; } = [];
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}