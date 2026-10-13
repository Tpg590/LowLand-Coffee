using Notification.API.Model;

namespace Notification.API.Repository;

public interface INotificationRepository
{
    Task AddAsync(NotifNotification notification);
    Task AddLogAsync(NotifNotificationLog log);
    Task SaveChangesAsync();

    Task<List<NotifNotification>> GetInboxAsync(Guid userId);
    Task<NotifNotification?> GetByIdAsync(Guid id);
    Task<List<NotifNotificationLog>> GetLogsAsync(
        Guid? notificationId,
        int page,
        int pageSize);
    Task DeleteAsync(NotifNotification notification);

}