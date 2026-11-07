using Order.API.DTOs;

namespace Order.API.Services
{
    public interface INotificationClient
    {
        Task SendOrderNotificationAsync(
            SendOrderNotificationRequest request,
            CancellationToken cancellationToken = default);
    }
}
