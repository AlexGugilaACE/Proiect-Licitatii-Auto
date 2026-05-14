namespace AutoAuction.Domain.Enums;

public enum NotificationType
{
    NewBid = 0,
    Outbid = 1,
    AuctionWon = 2,
    AuctionLost = 3,
    AuctionEnded = 4,
    TransactionCreated = 5
}
