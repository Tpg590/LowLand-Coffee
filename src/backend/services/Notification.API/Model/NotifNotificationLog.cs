using System;
using System.Collections.Generic;

namespace Notification.API.Model;

public partial class NotifNotificationLog
{
    public Guid Id { get; set; }

    public Guid? NotificationId { get; set; }

    public string Channel { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public DateTime SentAt { get; set; }

    public virtual NotifNotification? Notification { get; set; }
}
