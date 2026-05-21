using AutoAuction.Domain.Entities;

namespace AutoAuction.Application.Interfaces;

public interface IFavoriteService
{
    Task<int> CleanupInactiveFavoritesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Favorite>> GetUserFavoritesAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsFavoriteAsync(string userId, int auctionId, CancellationToken cancellationToken = default);
    Task ToggleAsync(string userId, int auctionId, CancellationToken cancellationToken = default);
}
