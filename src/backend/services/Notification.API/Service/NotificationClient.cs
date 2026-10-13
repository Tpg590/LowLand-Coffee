using System.Net.Http.Json;
namespace Notification.API.Service
{
    public class NotificationClient : INotificationClient
    {
        private readonly HttpClient _httpClient;

        public NotificationClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendOrderNotificationAsync(
            Guid userId,
            Guid orderId,
            string title,
            string content)
        {
            var request = new
            {
                UserId = userId,
                OrderId = orderId,
                Title = title,
                Content = content
            };

            using var response = await _httpClient.PostAsJsonAsync(
                "api/notifications/order",
                request);

            response.EnsureSuccessStatusCode();
        }
    }
}
