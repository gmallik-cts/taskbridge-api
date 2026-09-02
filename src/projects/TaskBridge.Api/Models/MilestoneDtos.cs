using System.ComponentModel.DataAnnotations;

namespace TaskBridge.Api.Models;

public sealed class CreateMilestoneRequest
{
    [Required] public Guid ProjectId { get; set; }
    [Required, StringLength(200, MinimumLength = 1)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Planned;
}

public sealed class UpdateMilestoneStatusRequest
{
    [Required] public Guid ConcurrencyToken { get; set; }
    public MilestoneStatus Status { get; set; }
}

public sealed class MilestoneResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public MilestoneStatus Status { get; init; }
    public Guid ConcurrencyToken { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }

    public static MilestoneResponse FromEntity(Milestone milestone) => new()
    {
        Id = milestone.Id, OrganizationId = milestone.OrganizationId, ProjectId = milestone.ProjectId,
        Name = milestone.Name, Description = milestone.Description, Status = milestone.Status,
        ConcurrencyToken = milestone.ConcurrencyToken, CreatedAtUtc = milestone.CreatedAtUtc, UpdatedAtUtc = milestone.UpdatedAtUtc
    };
}

public sealed class ReopenMilestoneRequest
{
    [Required] public Guid ConcurrencyToken { get; set; }
}