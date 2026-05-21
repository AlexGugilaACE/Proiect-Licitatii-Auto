using AutoAuction.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace AutoAuction.Web.Models;

public class SellerReviewSummaryViewModel
{
    public string SellerId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal RatingAverage { get; set; }
    public int RatingCount { get; set; }
}

public class SellerReviewsViewModel
{
    public SellerReviewSummaryViewModel Seller { get; set; } = new();
    public IReadOnlyList<ReviewListItemViewModel> Reviews { get; set; } = [];
    public CreateReviewViewModel NewReview { get; set; } = new();
    public bool CanReview { get; set; }
    public bool HasOwnReview { get; set; }
}

public class ReviewListItemViewModel
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string? AuctionTitle { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewViewModel
{
    [Required]
    public string SellerId { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Ratingul trebuie sa fie intre 1 si 5.")]
    public int Rating { get; set; } = 5;

    [StringLength(1200, ErrorMessage = "Review-ul poate avea maximum 1200 de caractere.")]
    public string Comment { get; set; } = string.Empty;
}
