using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task MarkReadAsync_marks_only_the_current_users_notification()
    {
        await using var db = CreateDbContext();
        var ownNotification = new Notification
        {
            UserId = "buyer-1",
            Title = "Test",
            Message = "Own notification",
            Type = NotificationType.Outbid
        };
        var otherNotification = new Notification
        {
            UserId = "buyer-2",
            Title = "Test",
            Message = "Other notification",
            Type = NotificationType.Outbid
        };
        db.Notifications.AddRange(ownNotification, otherNotification);
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        var ownResult = await service.MarkReadAsync(ownNotification.Id, "buyer-1");
        var otherResult = await service.MarkReadAsync(otherNotification.Id, "buyer-1");

        Assert.True(ownResult);
        Assert.False(otherResult);
        Assert.True(ownNotification.IsRead);
        Assert.False(otherNotification.IsRead);
    }

    [Fact]
    public async Task MarkAllReadAsync_marks_all_unread_notifications_for_user()
    {
        await using var db = CreateDbContext();
        db.Notifications.AddRange(
            new Notification { UserId = "buyer-1", Title = "A", Message = "A", Type = NotificationType.NewBid },
            new Notification { UserId = "buyer-1", Title = "B", Message = "B", Type = NotificationType.AuctionWon },
            new Notification { UserId = "buyer-2", Title = "C", Message = "C", Type = NotificationType.AuctionLost });
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        await service.MarkAllReadAsync("buyer-1");

        Assert.Equal(0, await service.GetUnreadCountAsync("buyer-1"));
        Assert.Equal(1, await service.GetUnreadCountAsync("buyer-2"));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
