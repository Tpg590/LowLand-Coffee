namespace Order.API.DTOs
{
    public class SendOrderNotificationRequest
    {
        public Guid UserId { get; set; }

        public Guid OrderId { get; set; }

        public string OrderStatus { get; set; } = string.Empty;
    }
}
