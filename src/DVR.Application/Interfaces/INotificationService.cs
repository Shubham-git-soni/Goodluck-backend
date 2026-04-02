namespace DVR.Application.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(int userId, string title, string body, string? type = null, string? referenceId = null, string? referenceType = null, string? actionUrl = null);
    Task SendToRoleAsync(string role, string title, string body, string? type = null, string? actionUrl = null);
    Task SendPushNotificationAsync(string deviceToken, string title, string body);
}
