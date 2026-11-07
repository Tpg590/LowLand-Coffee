using Order.API.DTOs;

namespace Order.API.Services
{
    public class NotificationClient : INotificationClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationClient> _logger;

        public NotificationClient(
            HttpClient httpClient,
            ILogger<NotificationClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendOrderNotificationAsync(
            SendOrderNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "api/notifications/orders",
                    request,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent =
                        await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogWarning(
                        "Notification API returned {StatusCode}. Response: {Response}",
                        (int)response.StatusCode,
                        responseContent);

                    return;
                }

                _logger.LogInformation(
                    "Order notification sent successfully. OrderId: {OrderId}",
                    request.OrderId);
            }
            catch (Exception ex)
            {
                // Không để lỗi Notification làm thất bại việc cập nhật đơn hàng.
                _logger.LogError(
                    ex,
                    "Failed to call Notification API for OrderId: {OrderId}",
                    request.OrderId);
            }
        }
        
    }
}