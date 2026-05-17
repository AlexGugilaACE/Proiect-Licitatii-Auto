using AutoAuction.Domain.Enums;

namespace AutoAuction.Domain.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public bool SellerConfirmed { get; set; }
    public bool BuyerConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}
