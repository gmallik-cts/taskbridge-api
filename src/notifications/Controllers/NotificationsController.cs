using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBridge.Notifications.DTOs;
using TaskBridge.Notifications.Services;

namespace TaskBridge.Notifications.Controllers;

[ApiController, Route("notifications"), Authorize(Policy = "TenantAccess")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<NotificationPageResponse>> Get(Guid userId, bool? isRead, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await service.GetAsync(userId, isRead, pageNumber, pageSize, cancellationToken));

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<NotificationResponse>> MarkRead(Guid id, MarkNotificationReadRequest? request, CancellationToken cancellationToken)
    {
        if (request is not null && !request.IsRead) throw new ArgumentException("Only marking a notification as read is supported.");
        return Ok(await service.MarkReadAsync(id, cancellationToken));
    }
}