using System.ComponentModel.DataAnnotations;

namespace AutoAuction.Web.Models;

public class RegisterViewModel
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Denumire firma")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "CUI firma")]
    public string FiscalCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Adresa firma")]
    public string CompanyAddress { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Cumparator";
}
