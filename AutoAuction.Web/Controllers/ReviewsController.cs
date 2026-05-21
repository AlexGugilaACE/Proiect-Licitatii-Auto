using AutoAuction.Domain.Entities;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

public class ReviewsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Seller(string id, CancellationToken cancellationToken)
    {
        var seller = await userManager.FindByIdAsync(id);
        if (seller is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var reviews = await db.Reviews
            .AsNoTracking()
            .Include(x => x.Auction)
            .Where(x => x.SellerId == id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var buyerIds = reviews.Select(x => x.BuyerId).Distinct().ToList();
        var buyers = await db.Users
            .AsNoTracking()
            .Where(x => buyerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var model = new SellerReviewsViewModel
        {
            Seller = await BuildSellerSummaryAsync(seller, cancellationToken),
            Reviews = reviews.Select(review =>
            {
                buyers.TryGetValue(review.BuyerId, out var buyer);
                var buyerName = buyer is null
                    ? "Utilizator"
                    : $"{buyer.FirstName} {buyer.LastName}".Trim();

                return new ReviewListItemViewModel
                {
                    Rating = review.Rating,
                    Comment = review.Comment,
                    BuyerName = string.IsNullOrWhiteSpace(buyerName) ? buyer?.Email ?? "Utilizator" : buyerName,
                    AuctionTitle = review.Auction?.Title,
                    CreatedAt = review.CreatedAt
                };
            }).ToList(),
            NewReview = new CreateReviewViewModel { SellerId = id, Rating = 5 },
            CanReview = User.Identity?.IsAuthenticated == true && currentUserId != id,
            HasOwnReview = currentUserId is not null && reviews.Any(x => x.BuyerId == currentUserId)
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewReview")] CreateReviewViewModel model, CancellationToken cancellationToken)
    {
        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (buyerId == model.SellerId)
        {
            ModelState.AddModelError(string.Empty, "Nu poti lasa review pentru propriul cont.");
        }

        var seller = await userManager.FindByIdAsync(model.SellerId);
        if (seller is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Seller), new { id = model.SellerId });
        }

        var existingReview = await db.Reviews
            .FirstOrDefaultAsync(x => x.SellerId == model.SellerId && x.BuyerId == buyerId, cancellationToken);

        if (existingReview is null)
        {
            db.Reviews.Add(new Review
            {
                SellerId = model.SellerId,
                BuyerId = buyerId,
                Rating = model.Rating,
                Comment = model.Comment.Trim()
            });
        }
        else
        {
            existingReview.Rating = model.Rating;
            existingReview.Comment = model.Comment.Trim();
            existingReview.CreatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await RefreshSellerRatingAsync(model.SellerId, cancellationToken);

        TempData["Success"] = existingReview is null ? "Review-ul a fost adaugat." : "Review-ul tau a fost actualizat.";
        return RedirectToAction(nameof(Seller), new { id = model.SellerId });
    }

    private async Task<SellerReviewSummaryViewModel> BuildSellerSummaryAsync(ApplicationUser seller, CancellationToken cancellationToken)
    {
        var dealerProfile = await db.DealerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == seller.Id, cancellationToken);
        var fallbackName = $"{seller.FirstName} {seller.LastName}".Trim();

        return new SellerReviewSummaryViewModel
        {
            SellerId = seller.Id,
            CompanyName = !string.IsNullOrWhiteSpace(dealerProfile?.CompanyName)
                ? dealerProfile.CompanyName
                : string.IsNullOrWhiteSpace(fallbackName) ? seller.Email ?? "Vanzator" : fallbackName,
            Email = seller.Email ?? string.Empty,
            RatingAverage = seller.RatingAverage,
            RatingCount = seller.RatingCount
        };
    }

    private async Task RefreshSellerRatingAsync(string sellerId, CancellationToken cancellationToken)
    {
        var ratings = await db.Reviews
            .Where(x => x.SellerId == sellerId)
            .Select(x => x.Rating)
            .ToListAsync(cancellationToken);

        var seller = await userManager.FindByIdAsync(sellerId);
        if (seller is null)
        {
            return;
        }

        seller.RatingCount = ratings.Count;
        seller.RatingAverage = ratings.Count == 0 ? 0 : Math.Round((decimal)ratings.Average(), 1);
        await userManager.UpdateAsync(seller);
    }
}
