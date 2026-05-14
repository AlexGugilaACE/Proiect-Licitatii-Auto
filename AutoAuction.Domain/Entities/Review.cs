namespace AutoAuction.Domain.Entities;

public class Review
{
    public int Id { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
