using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoAuction.Web.Models;

public class AuctionFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Titlul este obligatoriu.")]
    [StringLength(160, MinimumLength = 5, ErrorMessage = "Titlul trebuie să aibă între 5 și 160 de caractere.")]
    [Display(Name = "Titlu")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Descrierea este obligatorie.")]
    [StringLength(4000, MinimumLength = 20, ErrorMessage = "Descrierea trebuie să aibă între 20 și 4000 de caractere.")]
    [Display(Name = "Descriere")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "VIN-ul este obligatoriu.")]
    [StringLength(17, MinimumLength = 17)]
    [RegularExpression("^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$", ErrorMessage = "VIN-ul trebuie să aibă 17 caractere și să nu conțină I, O sau Q.")]
    [Display(Name = "VIN")]
    public string Vin { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Selectează marca.")]
    [Display(Name = "Marca")]
    public int BrandId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selectează modelul.")]
    [Display(Name = "Model")]
    public int CarModelId { get; set; }

    [Range(1950, 2100, ErrorMessage = "Anul trebuie să fie între 1950 și 2100.")]
    [Display(Name = "An")]
    public int Year { get; set; } = DateTime.Now.Year;

    [Range(0, 2_000_000, ErrorMessage = "Kilometrajul trebuie să fie între 0 și 2.000.000 km.")]
    [Display(Name = "Kilometraj")]
    public int Mileage { get; set; }

    [Range(500, 10_000, ErrorMessage = "Capacitatea cilindrică trebuie să fie între 500 și 10.000 cmc.")]
    [Display(Name = "Capacitate cilindrică")]
    public int EngineCapacityCm3 { get; set; } = 1_600;

    [Range(20, 2_000, ErrorMessage = "Puterea trebuie să fie între 20 și 2.000 CP.")]
    [Display(Name = "Putere")]
    public int HorsePower { get; set; } = 120;

    [Range(1, int.MaxValue, ErrorMessage = "Selectează combustibilul.")]
    public int FuelTypeId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selectează transmisia.")]
    public int TransmissionTypeId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selectează caroseria.")]
    public int BodyTypeId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selectează starea mașinii.")]
    public int ConditionId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selectează tracțiunea.")]
    public int DriveTypeId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Selectează culoarea.")]
    public int ColorId { get; set; }

    [Range(100, 100_000_000, ErrorMessage = "Prețul de pornire trebuie să fie între 100 și 100.000.000 EUR.")]
    [Display(Name = "Preț de pornire")]
    public decimal StartingPrice { get; set; }

    [Display(Name = "Start")]
    public DateTime StartTime { get; set; } = DateTime.Now;
    [Display(Name = "Final")]
    public DateTime EndTime { get; set; } = DateTime.Now.AddDays(3);

    [Range(10, 1_000_000, ErrorMessage = "Pasul minim de bid trebuie să fie între 10 și 1.000.000 EUR.")]
    [Display(Name = "Pas minim bid")]
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var currentYearLimit = DateTime.Now.Year + 1;
        if (Year > currentYearLimit)
        {
            yield return new ValidationResult($"Anul nu poate fi mai mare de {currentYearLimit}.", [nameof(Year)]);
        }

        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("Data de final trebuie să fie după data de start.", [nameof(EndTime)]);
        }

        if (!HasBids && EndTime <= DateTime.Now)
        {
            yield return new ValidationResult("Data de final trebuie să fie în viitor.", [nameof(EndTime)]);
        }

        if (MinimumBidIncrement >= StartingPrice)
        {
            yield return new ValidationResult("Pasul minim de bid trebuie să fie mai mic decât prețul de pornire.", [nameof(MinimumBidIncrement)]);
        }

        if (Images is not null)
        {
            foreach (var image in Images.Where(x => x.Length > 0))
            {
                if (image.Length > 5 * 1024 * 1024)
                {
                    yield return new ValidationResult("Fiecare poză trebuie să aibă maximum 5 MB.", [nameof(Images)]);
                    break;
                }

                var extension = Path.GetExtension(image.FileName);
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    yield return new ValidationResult("Sunt acceptate doar fișiere JPG, PNG sau WEBP.", [nameof(Images)]);
                    break;
                }
            }
        }
    }
}

public class AuctionImageViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsMainImage { get; set; }
}
