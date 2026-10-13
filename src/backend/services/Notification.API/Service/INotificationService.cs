using Notification.API.DTO;
using Notification.API.Model;
namespace Notification.API.Service;

public interface INotificationService
{
    Task SendOrderNotificationAsync(
        SendOrderNotificationRequest request);

    Task<int> BroadcastPromoAsync(
        BroadcastPromoRequest request);

    Task<List<NotificationResponse>> GetInboxAsync(Guid userId);

    Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);

    Task<bool> DeleteAsync(Guid userId, Guid notificationId);

    Task<List<NotificationLogResponse>> GetLogsAsync(
        Guid? notificationId,
        int page,
        int pageSize);
    Task<NotifNotification?> GetByIdAsync(Guid id, Guid userId);
}