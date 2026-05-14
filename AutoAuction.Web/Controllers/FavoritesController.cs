using AutoAuction.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoAuction.Web.Controllers;

[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var favorites = await favoriteService.GetUserFavoritesAsync(userId, cancellationToken);
        return View(favorites);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int auctionId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await favoriteService.ToggleAsync(userId, auctionId, cancellationToken);
        return RedirectToAction("Details", "Auctions", new { id = auctionId });
    }
}
