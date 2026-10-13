using Microsoft.EntityFrameworkCore;
using Notification.API.Model;

namespace Notification.API.Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly CoffeeNotificationDbContext _context;

    public NotificationRepository(CoffeeNotificationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotifNotification notification)
    {
        await _context.Set<NotifNotification>().AddAsync(notification);
    }

    public async Task AddLogAsync(NotifNotificationLog log)
    {
        await _context.Set<NotifNotificationLog>().AddAsync(log);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<NotifNotification>> GetInboxAsync(
        Guid userId)
    {
        return await _context.Set<NotifNotification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<NotifNotification?> GetByIdAsync(Guid id)
    {
        return await _context.Set<NotifNotification>()
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<NotifNotificationLog>> GetLogsAsync(
        Guid? notificationId,
        int page,
        int pageSize)
    {
        var query = _context.Set<NotifNotificationLog>()
            .AsNoTracking()
            .AsQueryable();

        if (notificationId.HasValue)
        {
            query = query.Where(
                log => log.NotificationId == notificationId.Value);
        }

        return await query
            .OrderByDescending(log => log.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task DeleteAsync(NotifNotification notification)
    {
        var logs = await _context.Set<NotifNotificationLog>()
            .Where(log => log.NotificationId == notification.Id)
            .ToListAsync();

        foreach (var log in logs)
        {
            log.NotificationId = null;
        }

        _context.Set<NotifNotification>().Remove(notification);
    }
}