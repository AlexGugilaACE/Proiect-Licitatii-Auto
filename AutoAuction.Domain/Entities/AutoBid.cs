namespace AutoAuction.Domain.Entities;

public class AutoBid
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public string BidderId { get; set; } = string.Empty;
    public decimal MaxAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
