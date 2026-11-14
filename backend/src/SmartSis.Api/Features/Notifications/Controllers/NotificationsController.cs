using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Controllers;

/// <summary>Any signed-in user: my in-app notifications (the bell, PB-12).</summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
[Tags("Notifications")]
public sealed class NotificationsController(INotificationInboxService inbox) : ControllerBase
{
    /// <summary>My notifications, newest first.</summary>
    [HttpGet]
    public Task<PagedResult<NotificationDto>> List([FromQuery] NotificationListQuery query, CancellationToken ct) =>
        inbox.ListMineAsync(query, ct);

    /// <summary>How many of my notifications are unread.</summary>
    [HttpGet("unread-count")]
    public Task<UnreadCountDto> UnreadCount(CancellationToken ct) => inbox.UnreadCountAsync(ct);

    /// <summary>Mark one of my notifications as read. Reading it again changes nothing.</summary>
    /// <response code="404">No notification of mine with this id.</response>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await inbox.MarkReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>Mark all my notifications as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await inbox.MarkAllReadAsync(ct);
        return NoContent();
    }
}
