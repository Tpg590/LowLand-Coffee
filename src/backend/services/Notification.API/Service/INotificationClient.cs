namespace Notification.API.Service
{
    public interface INotificationClient
    {
        Task SendOrderNotificationAsync(
            Guid userId,
            Guid orderId,
            string title,
            string content);
    }
}
