using Dapper;
using DVR.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DVR.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IDbConnectionFactory db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendToUserAsync(int userId, string title, string body,
        string? type = null, string? referenceId = null, string? referenceType = null)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO Notifications (UserId, Title, Body, Type, ReferenceId, ReferenceType, IsRead, CreatedAt)
            VALUES (@UserId, @Title, @Body, @Type, @ReferenceId, @ReferenceType, 0, GETUTCDATE())",
            new { UserId = userId, Title = title, Body = body, Type = type, ReferenceId = referenceId, ReferenceType = referenceType });

        var deviceToken = await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT DeviceToken FROM Users WHERE UserId = @UserId AND DeviceToken IS NOT NULL", new { UserId = userId });

        if (!string.IsNullOrEmpty(deviceToken))
            await SendPushNotificationAsync(deviceToken, title, body);
    }

    public async Task SendToRoleAsync(string role, string title, string body, string? type = null)
    {
        using var conn = _db.CreateConnection();
        var users = await conn.QueryAsync<int>(
            "SELECT UserId FROM Users WHERE Role = @Role AND IsActive = 1", new { Role = role });

        foreach (var userId in users)
            await SendToUserAsync(userId, title, body, type);
    }

    public Task SendPushNotificationAsync(string deviceToken, string title, string body)
    {
        _logger.LogInformation("Push notification to {DeviceToken}: {Title} - {Body}", deviceToken, title, body);
        return Task.CompletedTask;
    }
}
