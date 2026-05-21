using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Identity;
using AutoAuction.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

[Authorize]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(AppRoles.Administrator))
        {
            return RedirectToAction("Index", "Admin");
        }

        if (User.IsInRole(AppRoles.Seller))
        {
            return RedirectToAction(nameof(Seller));
        }

        return RedirectToAction(nameof(Buyer));
    }

    [Authorize(Roles = AppRoles.Seller)]
    public async Task<IActionResult> Seller(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var model = new SellerDashboardViewModel
        {
            AuctionCount = await db.Auctions.CountAsync(x => x.SellerId == userId, cancellationToken),
            ActiveAuctionCount = await db.Auctions.CountAsync(x => x.SellerId == userId && x.Status == AuctionStatus.Active, cancellationToken),
            EndedAuctionCount = await db.Auctions.CountAsync(x => x.SellerId == userId && (x.Status == AuctionStatus.Ended || x.Status == AuctionStatus.Unsold), cancellationToken),
            TransactionCount = await db.Transactions.CountAsync(x => x.SellerId == userId, cancellationToken),
            RecentAuctions = await db.Auctions
                .Include(x => x.Brand)
                .Include(x => x.CarModel)
                .Include(x => x.Images)
                .Where(x => x.SellerId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken),
            RecentTransactions = await db.Transactions
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.Images)
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.Brand)
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.CarModel)
                .Where(x => x.SellerId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }

    [Authorize(Roles = AppRoles.Buyer)]
    public async Task<IActionResult> Buyer(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var recentUserBids = await db.Bids
            .Include(x => x.Auction)
                .ThenInclude(x => x!.Brand)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.CarModel)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.Images)
            .Where(x => x.BidderId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var model = new BuyerDashboardViewModel
        {
            BidCount = await db.Bids.CountAsync(x => x.BidderId == userId, cancellationToken),
            FavoriteCount = await db.Favorites.CountAsync(x => x.UserId == userId, cancellationToken),
            WonTransactionCount = await db.Transactions.CountAsync(x => x.BuyerId == userId, cancellationToken),
            RecentBidAuctions = recentUserBids
                .Where(x => x.Auction is not null)
                .GroupBy(x => x.AuctionId)
                .Select(group =>
                {
                    var lastBid = group.OrderByDescending(x => x.CreatedAt).First();
                    var auction = lastBid.Auction!;
                    return new BuyerBidAuctionViewModel
                    {
                        AuctionId = auction.Id,
                        AuctionTitle = auction.Title,
                        VehicleName = $"{auction.Brand?.Name} {auction.CarModel?.Name}".Trim(),
                        ImagePath = auction.Images.OrderByDescending(image => image.IsMainImage).ThenBy(image => image.SortOrder).FirstOrDefault()?.FilePath,
                        CurrentPrice = auction.CurrentPrice,
                        HighestOwnBid = group.Max(x => x.Amount),
                        OwnBidCount = group.Count(),
                        LastBidAt = lastBid.CreatedAt
                    };
                })
                .OrderByDescending(x => x.LastBidAt)
                .Take(5)
                .ToList(),
            Favorites = await db.Favorites
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.Brand)
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.CarModel)
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.Images)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken),
            RecentTransactions = await db.Transactions
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.Images)
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.Brand)
                .Include(x => x.Auction)
                    .ThenInclude(x => x!.CarModel)
                .Where(x => x.BuyerId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync(cancellationToken)
        };

        return View(model);
    }
}
