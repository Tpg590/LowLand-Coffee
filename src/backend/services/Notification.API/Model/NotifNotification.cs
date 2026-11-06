using System;
using System.Collections.Generic;

namespace Notification.API.Model;

public partial class NotifNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Type { get; set; } = null!;

    public Guid? ReferenceId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<NotifNotificationLog> NotifNotificationLogs { get; set; } = new List<NotifNotificationLog>();
}
