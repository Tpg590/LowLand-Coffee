using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.API.DTO;
using Notification.API.Service;
using System.Security.Claims;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]

public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationController(INotificationService service)
    {
        _service = service;
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out userId);
    }

    [HttpPost("orders")]
    public async Task<IActionResult> SendOrderNotification(
        [FromBody] SendOrderNotificationRequest request)
    {
        await _service.SendOrderNotificationAsync(request);
        return Ok(new { message = "Order notification processed." });
    }

    
    [HttpPost("promotions/broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BroadcastPromo(
        [FromBody] BroadcastPromoRequest request)
    {
        var count = await _service.BroadcastPromoAsync(request);

        return Ok(new
        {
            message = "Promotion notifications created.",
            recipientCount = count
        });
    }

    
    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _service.GetInboxAsync(userId);
        return Ok(result);
    }

   
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var success = await _service.MarkAsReadAsync(userId, id);

        if (!success)
            return NotFound(new { message = "Notification not found." });

        return Ok(new { message = "Notification marked as read." });
    }

    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var success = await _service.DeleteAsync(userId, id);

        if (!success)
            return NotFound(new { message = "Notification not found." });

        return Ok(new { message = "Notification deleted." });
    }

    
    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? notificationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetLogsAsync(
            notificationId, page, pageSize);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userIdValue =
            User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier
            )?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var notification =
            await _service.GetByIdAsync(id, userId);

        if (notification == null)
            return NotFound(new
            {
                message = "Không tìm thấy thông báo."
            });

        return Ok(new
        {
          
            title = notification.Title,
            message = notification.Type,
            createdAt = notification.CreatedAt,
            isRead = notification.IsRead
        });
    }
}