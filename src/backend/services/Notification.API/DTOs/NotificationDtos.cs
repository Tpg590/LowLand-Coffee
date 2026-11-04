namespace Notification.API.DTO;

public class SendOrderNotificationRequest
{
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
}

public class BroadcastPromoRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<Guid> UserIds { get; set; } = new();
}

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationLogResponse
{
    public Guid Id { get; set; }
    public Guid? NotificationId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}