using AutoAuction.Application.DTOs;
using AutoAuction.Domain.Entities;

namespace AutoAuction.Application.Interfaces;

public interface IAuctionService
{
    Task<IReadOnlyList<Auction>> GetActiveAuctionsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Auction>> SearchAuctionsAsync(AuctionSearchDto search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Auction>> GetSellerAuctionsAsync(string sellerId, CancellationToken cancellationToken = default);
    Task<Auction?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<Auction> CreateAsync(string sellerId, AuctionCreateDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(int auctionId, string sellerId, AuctionCreateDto dto, CancellationToken cancellationToken = default);
    Task AddImagesAsync(int auctionId, IReadOnlyList<(string FileName, string FilePath)> images, CancellationToken cancellationToken = default);
    Task<BidResult> PlaceBidAsync(int auctionId, string bidderId, decimal amount, CancellationToken cancellationToken = default);
    Task CloseExpiredAuctionsAsync(CancellationToken cancellationToken = default);
}
