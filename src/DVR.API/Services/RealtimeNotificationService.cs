using DVR.API.Hubs;
using DVR.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace DVR.API.Services;

/// <summary>
/// Wrapper service that adds real-time SignalR notifications on top of database notifications
/// </summary>
public class RealtimeNotificationService : INotificationService
{
    private readonly INotificationService _inner;
    private readonly IHubContext<NotificationHub> _hubContext;

    public RealtimeNotificationService(
        INotificationService inner,
        IHubContext<NotificationHub> hubContext)
    {
        _inner = inner;
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(int userId, string title, string body,
        string? type = null, string? referenceId = null, string? referenceType = null, string? actionUrl = null)
    {
        // Save to database
        await _inner.SendToUserAsync(userId, title, body, type, referenceId, referenceType, actionUrl);

        // Send real-time notification via SignalR
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
        {
            title,
            body,
            type,
            actionUrl,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task SendToRoleAsync(string role, string title, string body, string? type = null, string? actionUrl = null)
    {
        // Save to database for all users with this role
        await _inner.SendToRoleAsync(role, title, body, type, actionUrl);

        // Send real-time notification via SignalR to role group
        await _hubContext.Clients.Group($"role_{role}").SendAsync("ReceiveNotification", new
        {
            title,
            body,
            type,
            actionUrl,
            timestamp = DateTime.UtcNow
        });
    }

    public Task SendPushNotificationAsync(string deviceToken, string title, string body)
        => _inner.SendPushNotificationAsync(deviceToken, title, body);
}
