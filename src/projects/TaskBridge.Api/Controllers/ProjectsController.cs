using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge.Api.Models;
using TaskBridge.Api.Services;

namespace TaskBridge.Api.Controllers;

[ApiController]
[Authorize(Policy = "TenantAccess")]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject([FromBody] Project project, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _projectService.CreateAsync(project, cancellationToken);
            return CreatedAtAction(nameof(GetByTeam), new { teamId = created.TeamId }, created);
        }
        catch (ArgumentException ex)
        {
            return ValidationProblem(ex.Message);
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<Project>> UpdateStatus(Guid id, [FromBody] ProjectStatus status, CancellationToken cancellationToken)
    {
        var project = await _projectService.UpdateStatusAsync(id, status, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    [HttpGet("team/{teamId:guid}")]
    public async Task<ActionResult<List<Project>>> GetByTeam(Guid teamId, CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetByTeamAsync(teamId, cancellationToken);
        return Ok(projects);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _projectService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
