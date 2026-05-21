namespace AutoAuction.Application.DTOs;

public class AutoBidProcessingResult
{
    public int AuctionId { get; init; }
    public decimal CurrentPrice { get; init; }
    public IReadOnlyList<string> OutbidUserIds { get; init; } = [];
    public DateTime BidCreatedAt { get; init; }
}
