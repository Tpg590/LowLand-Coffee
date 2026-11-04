using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Notification.API.DTO;
using Notification.API.Helpers;
using Notification.API.Hub;
using Notification.API.Model;
using Notification.API.Repository;
namespace Notification.API.Service;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repository,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendOrderNotificationAsync(
        SendOrderNotificationRequest request)
    {
        if (request.UserId == Guid.Empty ||
            request.OrderId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.OrderStatus))
        {
            throw new ArgumentException("Invalid order notification data.");
        }

        var notification = new NotifNotification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = "Order Update",
            Content = $"Your order status is now: {request.OrderStatus}",
            Type = "Order",
            ReferenceId = request.OrderId,
            IsRead = false,
            CreatedAt = VietnamTime.Now()
        };

        await _repository.AddAsync(notification);
        await _repository.SaveChangesAsync();

        await SendAndLogAsync(notification);
    }

   
    public async Task<int> BroadcastPromoAsync(
        BroadcastPromoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException(
                "Title and Content are required.");
        }

        var userIds = request.UserIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one recipient is required.");
        }

        var createdAt = VietnamTime.Now();
        var notifications = userIds.Select(userId =>
            new NotifNotification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Content = request.Content,
                Type = "Promotion",
                ReferenceId = null,
                IsRead = false,
                CreatedAt = createdAt
            }).ToList();

        foreach (var notification in notifications)
        {
            await _repository.AddAsync(notification);
        }

        await _repository.SaveChangesAsync();

        foreach (var notification in notifications)
        {
            try
            {
                await SendAndLogAsync(notification);
            }
            catch (Exception ex)
            {
              
                _logger.LogError(
                    ex,
                    "Failed to process promo notification {Id}",
                    notification.Id);
            }
        }

        return notifications.Count;
    }

    public async Task<List<NotificationResponse>> GetInboxAsync(
        Guid userId)
    {
        var notifications = await _repository.GetInboxAsync(userId);

        return notifications.Select(n => new NotificationResponse
        {
            Id = n.Id,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type,
            ReferenceId = n.ReferenceId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt
        }).ToList();
    }

    public async Task<bool> MarkAsReadAsync(
        Guid userId,
        Guid notificationId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);

        if (notification == null || notification.UserId != userId)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _repository.SaveChangesAsync();
        }

        return true;
    }

   
    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid notificationId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);

        if (notification == null || notification.UserId != userId)
        {
            return false;
        }

      
        var context = notification;

        await _repository.DeleteAsync(context);
        await _repository.SaveChangesAsync();

        return true;
    }

    public async Task<List<NotificationLogResponse>> GetLogsAsync(
        Guid? notificationId,
        int page,
        int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var logs = await _repository.GetLogsAsync(
            notificationId, page, pageSize);

        return logs.Select(log => new NotificationLogResponse
        {
            Id = log.Id,
            NotificationId = log.NotificationId,
            Channel = log.Channel,
            Status = log.Status,
            ErrorMessage = log.ErrorMessage,
            SentAt = log.SentAt
        }).ToList();
    }

    
    private async Task SendAndLogAsync(
        NotifNotification notification)
    {
        var log = new NotifNotificationLog
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            Channel = "SignalR",
            Status = "Pending",
            SentAt = VietnamTime.Now()
        };

        try
        {
            await _hubContext.Clients
                .Group($"user:{notification.UserId}")
                .SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    title = notification.Title,
                    content = notification.Content,
                    type = notification.Type,
                    referenceId = notification.ReferenceId,
                    isRead = notification.IsRead,
                    createdAt = notification.CreatedAt
                });

            log.Status = "Sent";
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message;

            _logger.LogError(
                ex,
                "SignalR delivery failed for notification {Id}",
                notification.Id);
        }

        log.SentAt =  VietnamTime.Now();
        await _repository.AddLogAsync(log);
        await _repository.SaveChangesAsync();
    }

    public async Task<NotifNotification?> GetByIdAsync(
    Guid id,
    Guid userId)
    {
        var notification =
            await _repository.GetByIdAsync(id);

        if (notification == null)
            return null;

        if (notification.UserId != userId)
            return null;

        return notification;
    }
}