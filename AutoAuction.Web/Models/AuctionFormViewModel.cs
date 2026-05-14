using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoAuction.Web.Models;

public class AuctionFormViewModel
{
    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Marca")]
    public int BrandId { get; set; }

    [Display(Name = "Model")]
    public int CarModelId { get; set; }

    [Range(1950, 2100)]
    public int Year { get; set; } = DateTime.Now.Year;

    [Range(0, 2_000_000)]
    public int Mileage { get; set; }

    public int FuelTypeId { get; set; }
    public int TransmissionTypeId { get; set; }
    public int BodyTypeId { get; set; }
    public int ConditionId { get; set; }
    public int DriveTypeId { get; set; }
    public int ColorId { get; set; }

    [Range(1, 100_000_000)]
    public decimal StartingPrice { get; set; }

    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime EndTime { get; set; } = DateTime.Now.AddDays(3);

    [Range(1, 1_000_000)]
    public decimal MinimumBidIncrement { get; set; } = 100;

    public string OverallGrade { get; set; } = "B";
    public string ExteriorCondition { get; set; } = string.Empty;
    public string InteriorCondition { get; set; } = string.Empty;
    public string MechanicalCondition { get; set; } = string.Empty;
    public string TireCondition { get; set; } = string.Empty;
    public bool HasAccidentHistory { get; set; }
    public bool HasServiceHistory { get; set; }
    public string ConditionNotes { get; set; } = string.Empty;
    public IReadOnlyList<IFormFile> Images { get; set; } = [];

    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];
    public IReadOnlyList<SelectListItem> Models { get; set; } = [];
    public IReadOnlyList<SelectListItem> FuelTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> TransmissionTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> BodyTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Conditions { get; set; } = [];
    public IReadOnlyList<SelectListItem> DriveTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Colors { get; set; } = [];
}
