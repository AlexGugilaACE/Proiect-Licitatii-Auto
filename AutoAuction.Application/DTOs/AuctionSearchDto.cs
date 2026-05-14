namespace AutoAuction.Application.DTOs;

public class AuctionSearchDto
{
    public string? Query { get; set; }
    public int? BrandId { get; set; }
    public int? CarModelId { get; set; }
    public int? FuelTypeId { get; set; }
    public int? TransmissionTypeId { get; set; }
    public int? BodyTypeId { get; set; }
    public int? ConditionId { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
}
