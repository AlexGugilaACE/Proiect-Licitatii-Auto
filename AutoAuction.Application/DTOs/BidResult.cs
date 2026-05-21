namespace AutoAuction.Application.DTOs;

public class BidResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal? CurrentPrice { get; init; }
    public string? OutbidUserId { get; init; }
    public IReadOnlyList<string> OutbidUserIds { get; init; } = [];
    public DateTime? BidCreatedAt { get; init; }

    public static BidResult Success(decimal currentPrice, string? outbidUserId = null, DateTime? bidCreatedAt = null) => new()
    {
        Succeeded = true,
        CurrentPrice = currentPrice,
        OutbidUserId = outbidUserId,
        OutbidUserIds = string.IsNullOrWhiteSpace(outbidUserId) ? [] : [outbidUserId],
        BidCreatedAt = bidCreatedAt
    };

    public static BidResult Success(decimal currentPrice, IReadOnlyList<string> outbidUserIds, DateTime? bidCreatedAt = null) => new()
    {
        Succeeded = true,
        CurrentPrice = currentPrice,
        OutbidUserId = outbidUserIds.FirstOrDefault(),
        OutbidUserIds = outbidUserIds,
        BidCreatedAt = bidCreatedAt
    };

    public static BidResult Failure(string message) => new()
    {
        Succeeded = false,
        Message = message
    };
}
