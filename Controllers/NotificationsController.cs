using CampusLostAndFound.Data;
using CampusLostAndFound.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> Mine([FromQuery] bool unreadOnly = false)
    {
        var query = _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == CurrentUserId);
        if (unreadOnly) query = query.Where(n => n.ReadAt == null);

        var notifications = await query.OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToListAsync();
        return Ok(notifications.Select(NotificationDto.From).ToList());
    }

    [HttpPatch("{id:int}/read")]
    public async Task<ActionResult<NotificationDto>> MarkRead(int id)
    {
        var notification = await _db.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.UserId == CurrentUserId);
        if (notification is null)
            return Problem(statusCode: StatusCodes.Status404NotFound,
                detail: $"Notification {id} was not found for your account.");

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(NotificationDto.From(notification));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var now = DateTime.UtcNow;
        await _db.Notifications
            .Where(n => n.UserId == CurrentUserId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _db.Notifications
            .Where(n => n.Id == id && n.UserId == CurrentUserId)
            .ExecuteDeleteAsync();
        return deleted == 0
            ? Problem(statusCode: StatusCodes.Status404NotFound,
                detail: $"Notification {id} was not found for your account.")
            : NoContent();
    }
}
