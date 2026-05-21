using AutoAuction.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoAuction.Web.Models;

public class AuctionIndexViewModel
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
    public int? MinMileage { get; set; }
    public int? MaxMileage { get; set; }
    public int? MinEngineCapacityCm3 { get; set; }
    public int? MaxEngineCapacityCm3 { get; set; }
    public int? MinHorsePower { get; set; }
    public int? MaxHorsePower { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "ending";
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public IReadOnlyList<Auction> Auctions { get; set; } = [];
    public IReadOnlySet<int> FavoriteAuctionIds { get; set; } = new HashSet<int>();
    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];
    public IReadOnlyList<SelectListItem> Models { get; set; } = [];
    public IReadOnlyList<SelectListItem> FuelTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> TransmissionTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> BodyTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Conditions { get; set; } = [];
    public IReadOnlyList<SelectListItem> SortOptions { get; set; } =
    [
        new("Finalizare apropiata", "ending"),
        new("Cele mai noi", "newest"),
        new("Ieftin la scump", "price_asc"),
        new("Scump la ieftin", "price_desc")
    ];
}
