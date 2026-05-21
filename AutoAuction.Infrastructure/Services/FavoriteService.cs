using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class FavoriteService(ApplicationDbContext db) : IFavoriteService
{
    public async Task<int> CleanupInactiveFavoritesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var favorites = await db.Favorites
            .Include(x => x.Auction)
            .Where(x => x.UserId == userId)
            .Where(x =>
                x.Auction == null ||
                x.Auction.Status == AuctionStatus.Ended ||
                x.Auction.Status == AuctionStatus.Unsold ||
                x.Auction.Status == AuctionStatus.Cancelled ||
                x.Auction.EndTime <= now)
            .ToListAsync(cancellationToken);

        foreach (var favorite in favorites)
        {
            var auctionTitle = favorite.Auction?.Title ?? "O licitatie salvata";
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Licitatia favorita s-a incheiat",
                Message = $"Licitatia \"{auctionTitle}\" a fost eliminata din favorite deoarece nu mai este activa.",
                Type = NotificationType.AuctionEnded
            });
        }

        db.Favorites.RemoveRange(favorites);
        await db.SaveChangesAsync(cancellationToken);
        return favorites.Count;
    }

    public async Task<IReadOnlyList<Favorite>> GetUserFavoritesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await db.Favorites
            .Include(x => x.Auction)
                .ThenInclude(x => x!.Brand)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.CarModel)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.FuelType)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.TransmissionType)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.Images)
            .Where(x => x.UserId == userId)
            .Where(x => x.Auction != null && x.Auction.Status == AuctionStatus.Active && x.Auction.EndTime > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsFavoriteAsync(string userId, int auctionId, CancellationToken cancellationToken = default)
    {
        return db.Favorites.AnyAsync(x => x.UserId == userId && x.AuctionId == auctionId, cancellationToken);
    }

    public async Task ToggleAsync(string userId, int auctionId, CancellationToken cancellationToken = default)
    {
        var existing = await db.Favorites
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AuctionId == auctionId, cancellationToken);

        if (existing is null)
        {
            db.Favorites.Add(new Favorite
            {
                UserId = userId,
                AuctionId = auctionId
            });
        }
        else
        {
            db.Favorites.Remove(existing);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
