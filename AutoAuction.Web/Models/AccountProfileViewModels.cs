using System.ComponentModel.DataAnnotations;

namespace AutoAuction.Web.Models;

public class AccountProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsSeller { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public ProfileDetailsViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel ChangePassword { get; set; } = new();
}

public class ProfileDetailsViewModel
{
    [Required]
    [Display(Name = "Prenume")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nume")]
    public string LastName { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parola curenta")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parola noua")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parolele nu coincid.")]
    [Display(Name = "Confirma parola noua")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Parola noua")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parolele nu coincid.")]
    [Display(Name = "Confirma parola noua")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
