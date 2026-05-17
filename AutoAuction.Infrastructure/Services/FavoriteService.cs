using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class FavoriteService(ApplicationDbContext db) : IFavoriteService
{
    public async Task<IReadOnlyList<Favorite>> GetUserFavoritesAsync(string userId, CancellationToken cancellationToken = default)
    {
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
