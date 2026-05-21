using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class NotificationService(ApplicationDbContext db) : INotificationService
{
    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await db.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var notifications = await db.Notifications
            .Where(x => x.UserId == userId && x.IsRead)
            .ToListAsync(cancellationToken);

        db.Notifications.RemoveRange(notifications);
        await db.SaveChangesAsync(cancellationToken);
    }
}
