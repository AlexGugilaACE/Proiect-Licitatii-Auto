using AutoAuction.Domain.Enums;

namespace AutoAuction.Domain.Entities;

public class UserReport
{
    public int Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public ReportTargetType TargetType { get; set; }
    public int? AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public int? ReviewId { get; set; }
    public Review? Review { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
