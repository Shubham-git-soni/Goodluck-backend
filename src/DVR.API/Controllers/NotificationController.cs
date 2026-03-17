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

    public NotificationController(
        IDbConnectionFactory db,
        ICurrentUserService currentUser,
        IHubContext<NotificationHub> hubContext)
    {
        _db = db;
        _currentUser = currentUser;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Store FCM token for push notifications
    /// </summary>
    [HttpPost("fcm-token")]
    public async Task<IActionResult> SaveFCMToken([FromBody] FCMTokenRequest request)
    {
        using var conn = _db.CreateConnection();

        // Check if token already exists
        var existing = await conn.QueryFirstOrDefaultAsync<int?>(
            "SELECT Id FROM FCMTokens WHERE UserId = @UserId AND Token = @Token",
            new { _currentUser.UserId, request.FcmToken });

        if (!existing.HasValue)
        {
            // Insert new token
            await conn.ExecuteAsync(@"
                INSERT INTO FCMTokens (UserId, Token, DeviceType, CreatedAt, UpdatedAt)
                VALUES (@UserId, @Token, @DeviceType, GETUTCDATE(), GETUTCDATE())",
                new
                {
                    _currentUser.UserId,
                    Token = request.FcmToken,
                    DeviceType = "Web"
                });
        }
        else
        {
            // Update timestamp
            await conn.ExecuteAsync(@"
                UPDATE FCMTokens SET UpdatedAt = GETUTCDATE()
                WHERE Id = @Id",
                new { Id = existing.Value });
        }

        return Ok(ApiResponse.Ok("FCM token saved successfully"));
    }

    /// <summary>
    /// Send notification to user (Admin/Manager only)
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            // Send via SignalR (real-time)
            await _hubContext.Clients.Group($"user_{request.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    request.Title,
                    request.Body,
                    Timestamp = DateTime.UtcNow
                });

            // TODO: Also send via FCM if needed
            // This would require implementing FCM service with Firebase Admin SDK

            return Ok(ApiResponse.Ok("Notification sent successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail($"Failed to send notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// TEST ENDPOINT - Send notification without auth (REMOVE IN PRODUCTION!)
    /// </summary>
    [HttpPost("test-send")]
    [AllowAnonymous]
    public async Task<IActionResult> TestSendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            // Send via SignalR (real-time)
            await _hubContext.Clients.Group($"user_{request.UserId}")
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
            return BadRequest(ApiResponse.Fail($"Failed to send notification: {ex.Message}"));
        }
    }
}

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
