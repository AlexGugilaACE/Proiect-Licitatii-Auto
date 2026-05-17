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

    [Required]
    [StringLength(17, MinimumLength = 17)]
    [RegularExpression("^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$", ErrorMessage = "VIN-ul trebuie sa aiba 17 caractere si sa nu contina I, O sau Q.")]
    [Display(Name = "VIN")]
    public string Vin { get; set; } = string.Empty;

    [Display(Name = "Marca")]
    public int BrandId { get; set; }

    [Display(Name = "Model")]
    public int CarModelId { get; set; }

    [Range(1950, 2100)]
    public int Year { get; set; } = DateTime.Now.Year;

    [Range(0, 2_000_000)]
    public int Mileage { get; set; }

    [Range(500, 10_000)]
    [Display(Name = "Capacitate cilindrica")]
    public int EngineCapacityCm3 { get; set; } = 1_600;

    [Range(20, 2_000)]
    [Display(Name = "Putere")]
    public int HorsePower { get; set; } = 120;

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
    public List<IFormFile>? Images { get; set; }
    public IReadOnlyList<AuctionImageViewModel> ExistingImages { get; set; } = [];
    public bool HasBids { get; set; }
    public bool IsFinalized { get; set; }

    public IReadOnlyList<SelectListItem> Brands { get; set; } = [];
    public IReadOnlyList<SelectListItem> Models { get; set; } = [];
    public IReadOnlyList<SelectListItem> FuelTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> TransmissionTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> BodyTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Conditions { get; set; } = [];
    public IReadOnlyList<SelectListItem> DriveTypes { get; set; } = [];
    public IReadOnlyList<SelectListItem> Colors { get; set; } = [];
}

public class AuctionImageViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsMainImage { get; set; }
}
