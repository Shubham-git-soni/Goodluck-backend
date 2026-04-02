using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DVR.API.Hubs;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly INotificationService _notifications;

    public NotificationController(
        IDbConnectionFactory db,
        ICurrentUserService currentUser,
        IHubContext<NotificationHub> hubContext,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _hubContext = hubContext;
        _notifications = notifications;
    }

    // ─── GET notifications for current user ───────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "n.UserId = @UserId" };
        var p = new DynamicParameters();
        p.Add("UserId", _currentUser.UserId);
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (unreadOnly == true) where.Add("n.IsRead = 0");

        var wc = string.Join(" AND ", where);
        var total = await conn.QueryFirstOrDefaultAsync<int>(
            $"SELECT COUNT(*) FROM Notifications n WHERE {wc}", p);
        var unreadCount = await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Notifications WHERE UserId = @UserId AND IsRead = 0",
            new { _currentUser.UserId });
        var data = await conn.QueryAsync(
            $"SELECT * FROM Notifications n WHERE {wc} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new
        {
            success = true,
            message = "Notifications retrieved.",
            data,
            unreadCount,
            pagination = new { page, pageSize, totalCount = total }
        });
    }

    // ─── Mark as read ─────────────────────────────────────────────────────────

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @id AND UserId = @UserId",
            new { id, _currentUser.UserId });
        return rows > 0
            ? Ok(ApiResponse.Ok("Notification marked as read."))
            : NotFound(ApiResponse.Fail("Notification not found."));
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0",
            new { _currentUser.UserId });
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM Notifications WHERE NotificationId = @id AND UserId = @UserId",
            new { id, _currentUser.UserId });
        return rows > 0
            ? Ok(ApiResponse.Ok("Notification deleted."))
            : NotFound(ApiResponse.Fail("Notification not found."));
    }

    // ─── Send to specific user (Admin/Manager only) ───────────────────────────

    [HttpPost("send")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SendToUser([FromBody] SendNotificationRequest request)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user_{request.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    request.Title,
                    request.Body,
                    Timestamp = DateTime.UtcNow
                });
            return Ok(ApiResponse.Ok("Notification sent successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail($"Failed: {ex.Message}"));
        }
    }

    // ─── Send to all users with a role (salesman → admin notification) ────────

    [HttpPost("send-to-role")]
    public async Task<IActionResult> SendToRole([FromBody] SendToRoleRequest request)
    {
        try
        {
            await _hubContext.Clients
                .Group($"role_{request.Role}")
                .SendAsync("ReceiveNotification", new
                {
                    request.Title,
                    request.Body,
                    Timestamp = DateTime.UtcNow
                });
            return Ok(ApiResponse.Ok($"Notification sent to role {request.Role}"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail($"Failed: {ex.Message}"));
        }
    }

    // ─── FCM device token (push notifications) ────────────────────────────────

    [HttpPost("fcm-token")]
    public async Task<IActionResult> SaveFCMToken([FromBody] FCMTokenRequest request)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM FCMTokens WHERE UserId = @UserId AND Token = @Token",
            new { _currentUser.UserId, Token = request.FcmToken });

        if (!existing.HasValue)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO FCMTokens (UserId, Token, DeviceType, CreatedAt, UpdatedAt)
                VALUES (@UserId, @Token, @DeviceType, GETUTCDATE(), GETUTCDATE())",
                new { _currentUser.UserId, Token = request.FcmToken, DeviceType = "Web" });
        }
        else
        {
            await conn.ExecuteAsync(
                "UPDATE FCMTokens SET UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                new { Id = existing.Value });
        }

        return Ok(ApiResponse.Ok("FCM token saved successfully"));
    }

    // ─── Test endpoint (remove in production) ────────────────────────────────

    [HttpPost("test-send")]
    [AllowAnonymous]
    public async Task<IActionResult> TestSend([FromBody] SendNotificationRequest request)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user_{request.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    request.Title,
                    request.Body,
                    Timestamp = DateTime.UtcNow
                });
            return Ok(ApiResponse.Ok("Test notification sent successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail($"Failed: {ex.Message}"));
        }
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public class FCMTokenRequest
{
    public string FcmToken { get; set; } = string.Empty;
}

public class SendNotificationRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class SendToRoleRequest
{
    public string Role { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
