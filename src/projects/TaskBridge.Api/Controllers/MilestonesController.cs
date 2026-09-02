using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge.Api.Models;
using TaskBridge.Api.Services;

namespace TaskBridge.Api.Controllers;

[ApiController, Authorize(Policy = "TenantAccess"), Route("api/milestones")]
public sealed class MilestonesController(IMilestoneService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MilestoneResponse>> Create(CreateMilestoneRequest request, CancellationToken cancellationToken)
    {
        var milestone = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = milestone.Id }, milestone);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MilestoneResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var milestone = await service.GetAsync(id, cancellationToken);
        return milestone is null ? NotFound() : Ok(milestone);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<MilestoneResponse>> UpdateStatus(Guid id, UpdateMilestoneStatusRequest request, CancellationToken cancellationToken)
    {
        var milestone = await service.UpdateStatusAsync(id, request, cancellationToken);
        return milestone is null ? NotFound() : Ok(milestone);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}