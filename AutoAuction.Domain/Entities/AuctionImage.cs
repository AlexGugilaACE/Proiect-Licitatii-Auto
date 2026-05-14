namespace AutoAuction.Domain.Entities;

public class AuctionImage
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsMainImage { get; set; }
}
