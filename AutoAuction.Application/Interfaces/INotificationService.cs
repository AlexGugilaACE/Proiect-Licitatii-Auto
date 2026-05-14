using AutoAuction.Domain.Entities;

namespace AutoAuction.Application.Interfaces;

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetUserNotificationsAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
}
