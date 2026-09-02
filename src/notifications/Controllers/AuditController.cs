using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge.Notifications.DTOs;
using TaskBridge.Notifications.Services;

namespace TaskBridge.Notifications.Controllers;

[ApiController, Route("audit"), Authorize(Policy = "TenantAccess")]
public sealed class AuditController(IAuditService service) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "AuditIngestion")]
    public async Task<ActionResult<AuditCreateResponse>> Create(CreateAuditRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentException("Request body is required.");
        var response = await service.CreateAsync(request, cancellationToken);
        return response.IsDuplicate ? Ok(response) : StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<AuditPageResponse>> Get(Guid projectId, DateTime? from, DateTime? to, string? eventType, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await service.GetProjectAsync(projectId, from, to, eventType, pageNumber, pageSize, cancellationToken));
}