namespace DVR.Application.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(int userId, string title, string body, string? type = null, string? referenceId = null, string? referenceType = null);
    Task SendToRoleAsync(string role, string title, string body, string? type = null);
    Task SendPushNotificationAsync(string deviceToken, string title, string body);
}
