using Dapper;
using DVR.Application.Common;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DVR.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(IDbConnectionFactory db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? unreadOnly = null)
    {
        using var conn = _db.CreateConnection();
        var where = new List<string> { "n.UserId = @UserId" };
        var p = new DynamicParameters();
        p.Add("UserId", _currentUser.UserId);
        p.Add("Offset", (page - 1) * pageSize);
        p.Add("PageSize", pageSize);

        if (unreadOnly == true) { where.Add("n.IsRead = 0"); }

        var wc = string.Join(" AND ", where);
        var total = await conn.QueryFirstOrDefaultAsync<int>($"SELECT COUNT(*) FROM Notifications n WHERE {wc}", p);
        var unreadCount = await conn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Notifications WHERE UserId = @UserId AND IsRead = 0", new { _currentUser.UserId });
        var data = await conn.QueryAsync($"SELECT * FROM Notifications n WHERE {wc} ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", p);

        return Ok(new { success = true, message = "Notifications retrieved.", data, unreadCount,
            pagination = new { page, pageSize, totalCount = total } });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Notifications SET IsRead = 1 WHERE NotificationId = @id AND UserId = @UserId",
            new { id, _currentUser.UserId });

        return rows > 0 ? Ok(ApiResponse.Ok("Notification marked as read.")) : NotFound(ApiResponse.Fail("Notification not found."));
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE Notifications SET IsRead = 1 WHERE UserId = @UserId AND IsRead = 0", new { _currentUser.UserId });
        return Ok(ApiResponse.Ok("All notifications marked as read."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM Notifications WHERE NotificationId = @id AND UserId = @UserId",
            new { id, _currentUser.UserId });

        return rows > 0 ? Ok(ApiResponse.Ok("Notification deleted.")) : NotFound(ApiResponse.Fail("Notification not found."));
    }
}
