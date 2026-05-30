namespace AutoAuction.Domain.Entities;

public class AuctionQuestion
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public string BuyerId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
}
