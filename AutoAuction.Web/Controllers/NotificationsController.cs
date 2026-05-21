using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Enums;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

[Authorize]
public class NotificationsController(INotificationService notificationService) : Controller
{
    public async Task<IActionResult> Index(string filter = "all", CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var notifications = await notificationService.GetUserNotificationsAsync(userId, cancellationToken);
        filter = NormalizeFilter(filter);

        var model = new NotificationsViewModel
        {
            Filter = filter,
            TotalCount = notifications.Count,
            UnreadCount = notifications.Count(x => !x.IsRead),
            AuctionCount = notifications.Count(IsAuctionNotification),
            TransactionCount = notifications.Count(IsTransactionNotification),
            Notifications = filter switch
            {
                "unread" => notifications.Where(x => !x.IsRead).ToList(),
                "auctions" => notifications.Where(IsAuctionNotification).ToList(),
                "transactions" => notifications.Where(IsTransactionNotification).ToList(),
                _ => notifications
            }
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await notificationService.MarkAllReadAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRead(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await notificationService.DeleteReadAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await notificationService.MarkReadAsync(id, userId, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeFilter(string? filter)
    {
        return filter?.ToLowerInvariant() switch
        {
            "unread" => "unread",
            "auctions" => "auctions",
            "transactions" => "transactions",
            _ => "all"
        };
    }

    private static bool IsAuctionNotification(AutoAuction.Domain.Entities.Notification notification)
    {
        return notification.Type is NotificationType.NewBid or
            NotificationType.Outbid or
            NotificationType.AuctionWon or
            NotificationType.AuctionLost or
            NotificationType.AuctionEnded;
    }

    private static bool IsTransactionNotification(AutoAuction.Domain.Entities.Notification notification)
    {
        return notification.Type == NotificationType.TransactionCreated;
    }
}
