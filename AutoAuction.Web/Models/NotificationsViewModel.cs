using AutoAuction.Domain.Entities;

namespace AutoAuction.Web.Models;

public class NotificationsViewModel
{
    public IReadOnlyList<Notification> Notifications { get; set; } = [];
    public string Filter { get; set; } = "all";
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int AuctionCount { get; set; }
    public int TransactionCount { get; set; }
}
