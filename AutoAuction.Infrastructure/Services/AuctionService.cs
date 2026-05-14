using AutoAuction.Application.DTOs;
using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class AuctionService(ApplicationDbContext db) : IAuctionService
{
    public async Task<IReadOnlyList<Auction>> GetActiveAuctionsAsync(CancellationToken cancellationToken = default)
    {
        await ActivateScheduledAuctionsAsync(cancellationToken);

        return await IncludeAuctionDetails(db.Auctions)
            .Where(x => x.Status == AuctionStatus.Active || x.Status == AuctionStatus.Scheduled)
            .OrderBy(x => x.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Auction>> SearchAuctionsAsync(AuctionSearchDto search, CancellationToken cancellationToken = default)
    {
        await ActivateScheduledAuctionsAsync(cancellationToken);

        var query = IncludeAuctionDetails(db.Auctions)
            .Where(x => x.Status == AuctionStatus.Active || x.Status == AuctionStatus.Scheduled);

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            var text = search.Query.Trim();
            query = query.Where(x => x.Title.Contains(text) || x.Description.Contains(text));
        }

        if (search.BrandId is > 0)
        {
            query = query.Where(x => x.BrandId == search.BrandId.Value);
        }

        if (search.CarModelId is > 0)
        {
            query = query.Where(x => x.CarModelId == search.CarModelId.Value);
        }

        if (search.FuelTypeId is > 0)
        {
            query = query.Where(x => x.FuelTypeId == search.FuelTypeId.Value);
        }

        if (search.TransmissionTypeId is > 0)
        {
            query = query.Where(x => x.TransmissionTypeId == search.TransmissionTypeId.Value);
        }

        if (search.BodyTypeId is > 0)
        {
            query = query.Where(x => x.BodyTypeId == search.BodyTypeId.Value);
        }

        if (search.ConditionId is > 0)
        {
            query = query.Where(x => x.ConditionId == search.ConditionId.Value);
        }

        if (search.MinYear is not null)
        {
            query = query.Where(x => x.Year >= search.MinYear.Value);
        }

        if (search.MaxYear is not null)
        {
            query = query.Where(x => x.Year <= search.MaxYear.Value);
        }

        if (search.MinPrice is not null)
        {
            query = query.Where(x => x.CurrentPrice >= search.MinPrice.Value);
        }

        if (search.MaxPrice is not null)
        {
            query = query.Where(x => x.CurrentPrice <= search.MaxPrice.Value);
        }

        query = search.SortBy switch
        {
            "price_asc" => query.OrderBy(x => x.CurrentPrice),
            "price_desc" => query.OrderByDescending(x => x.CurrentPrice),
            "newest" => query.OrderByDescending(x => x.CreatedAt),
            "ending" => query.OrderBy(x => x.EndTime),
            _ => query.OrderBy(x => x.EndTime)
        };

        var page = Math.Max(search.Page, 1);
        var pageSize = search.PageSize is > 0 and <= 48 ? search.PageSize : 9;
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Auction>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<Auction>> GetSellerAuctionsAsync(string sellerId, CancellationToken cancellationToken = default)
    {
        return await IncludeAuctionDetails(db.Auctions)
            .Where(x => x.SellerId == sellerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Auction?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await ActivateScheduledAuctionsAsync(cancellationToken);

        return await IncludeAuctionDetails(db.Auctions)
            .Include(x => x.Bids.OrderByDescending(b => b.Amount))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Auction> CreateAsync(string sellerId, AuctionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Local).ToUniversalTime();
        var end = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Local).ToUniversalTime();

        var auction = new Auction
        {
            SellerId = sellerId,
            BrandId = dto.BrandId,
            CarModelId = dto.CarModelId,
            Title = dto.Title,
            Description = dto.Description,
            Year = dto.Year,
            Mileage = dto.Mileage,
            FuelTypeId = dto.FuelTypeId,
            TransmissionTypeId = dto.TransmissionTypeId,
            BodyTypeId = dto.BodyTypeId,
            ConditionId = dto.ConditionId,
            DriveTypeId = dto.DriveTypeId,
            ColorId = dto.ColorId,
            StartTime = start,
            EndTime = end,
            StartingPrice = dto.StartingPrice,
            CurrentPrice = dto.StartingPrice,
            MinimumBidIncrement = dto.MinimumBidIncrement,
            Status = start <= now ? AuctionStatus.Active : AuctionStatus.Scheduled,
            ConditionReport = new VehicleConditionReport
            {
                OverallGrade = dto.OverallGrade,
                ExteriorCondition = dto.ExteriorCondition,
                InteriorCondition = dto.InteriorCondition,
                MechanicalCondition = dto.MechanicalCondition,
                TireCondition = dto.TireCondition,
                HasAccidentHistory = dto.HasAccidentHistory,
                HasServiceHistory = dto.HasServiceHistory,
                Notes = dto.ConditionNotes
            }
        };

        db.Auctions.Add(auction);
        await db.SaveChangesAsync(cancellationToken);
        return auction;
    }

    public async Task AddImagesAsync(int auctionId, IReadOnlyList<(string FileName, string FilePath)> images, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < images.Count; i++)
        {
            db.AuctionImages.Add(new AuctionImage
            {
                AuctionId = auctionId,
                FileName = images[i].FileName,
                FilePath = images[i].FilePath,
                SortOrder = i,
                IsMainImage = i == 0
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int auctionId, string sellerId, AuctionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var auction = await db.Auctions
            .Include(x => x.ConditionReport)
            .FirstOrDefaultAsync(x => x.Id == auctionId && x.SellerId == sellerId, cancellationToken);

        if (auction is null || auction.Status is AuctionStatus.Ended or AuctionStatus.Cancelled)
        {
            return false;
        }

        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Local).ToUniversalTime();
        var end = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Local).ToUniversalTime();

        auction.BrandId = dto.BrandId;
        auction.CarModelId = dto.CarModelId;
        auction.Title = dto.Title;
        auction.Description = dto.Description;
        auction.Year = dto.Year;
        auction.Mileage = dto.Mileage;
        auction.FuelTypeId = dto.FuelTypeId;
        auction.TransmissionTypeId = dto.TransmissionTypeId;
        auction.BodyTypeId = dto.BodyTypeId;
        auction.ConditionId = dto.ConditionId;
        auction.DriveTypeId = dto.DriveTypeId;
        auction.ColorId = dto.ColorId;
        auction.StartTime = start;
        auction.EndTime = end;
        auction.StartingPrice = dto.StartingPrice;
        auction.MinimumBidIncrement = dto.MinimumBidIncrement;
        if (!await db.Bids.AnyAsync(x => x.AuctionId == auctionId, cancellationToken))
        {
            auction.CurrentPrice = dto.StartingPrice;
        }
        auction.Status = start <= DateTime.UtcNow ? AuctionStatus.Active : AuctionStatus.Scheduled;
        auction.UpdatedAt = DateTime.UtcNow;

        auction.ConditionReport ??= new VehicleConditionReport { AuctionId = auction.Id };
        auction.ConditionReport.OverallGrade = dto.OverallGrade;
        auction.ConditionReport.ExteriorCondition = dto.ExteriorCondition;
        auction.ConditionReport.InteriorCondition = dto.InteriorCondition;
        auction.ConditionReport.MechanicalCondition = dto.MechanicalCondition;
        auction.ConditionReport.TireCondition = dto.TireCondition;
        auction.ConditionReport.HasAccidentHistory = dto.HasAccidentHistory;
        auction.ConditionReport.HasServiceHistory = dto.HasServiceHistory;
        auction.ConditionReport.Notes = dto.ConditionNotes;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BidResult> PlaceBidAsync(int auctionId, string bidderId, decimal amount, CancellationToken cancellationToken = default)
    {
        var auction = await db.Auctions.FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);
        if (auction is null)
        {
            return BidResult.Failure("Licitatia nu exista.");
        }

        var now = DateTime.UtcNow;
        if (auction.SellerId == bidderId)
        {
            return BidResult.Failure("Nu poti licita la propria licitatie.");
        }

        if (auction.Status == AuctionStatus.Scheduled && auction.StartTime <= now)
        {
            auction.Status = AuctionStatus.Active;
        }

        if (auction.Status != AuctionStatus.Active || auction.StartTime > now || auction.EndTime <= now)
        {
            return BidResult.Failure("Licitatia nu este activa.");
        }

        var currentTopBid = await db.Bids
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentTopBid?.BidderId == bidderId)
        {
            return BidResult.Failure("Ai deja cea mai mare oferta pentru aceasta licitatie.");
        }

        var minimumAcceptedAmount = auction.CurrentPrice + auction.MinimumBidIncrement;
        if (amount < minimumAcceptedAmount)
        {
            return BidResult.Failure($"Oferta minima este {minimumAcceptedAmount:N2}.");
        }

        var previousTopBidderId = currentTopBid?.BidderId;

        var bid = new Bid
        {
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            CreatedAt = now
        };

        auction.CurrentPrice = amount;
        auction.UpdatedAt = now;
        db.Bids.Add(bid);

        db.Notifications.Add(new Notification
        {
            UserId = auction.SellerId,
            Title = "Oferta noua",
            Message = $"Licitatia {auction.Title} a primit o oferta de {amount:N2}.",
            Type = NotificationType.NewBid
        });

        if (!string.IsNullOrWhiteSpace(previousTopBidderId) && previousTopBidderId != bidderId)
        {
            db.Notifications.Add(new Notification
            {
                UserId = previousTopBidderId,
                Title = "Ai fost depasit",
                Message = $"A fost plasata o oferta mai mare pentru {auction.Title}.",
                Type = NotificationType.Outbid
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return BidResult.Success(auction.CurrentPrice, previousTopBidderId, bid.CreatedAt);
    }

    public async Task CloseExpiredAuctionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = await db.Auctions
            .Include(x => x.Bids)
            .Where(x => (x.Status == AuctionStatus.Active || x.Status == AuctionStatus.Scheduled) && x.EndTime <= now)
            .ToListAsync(cancellationToken);

        foreach (var auction in expired)
        {
            var winningBid = auction.Bids.OrderByDescending(x => x.Amount).FirstOrDefault();
            if (winningBid is null)
            {
                auction.Status = AuctionStatus.Unsold;
                continue;
            }

            auction.Status = AuctionStatus.Ended;
            auction.WinningBidId = winningBid.Id;
            db.Transactions.Add(new Domain.Entities.Transaction
            {
                AuctionId = auction.Id,
                SellerId = auction.SellerId,
                BuyerId = winningBid.BidderId,
                Amount = winningBid.Amount
            });

            db.Notifications.Add(new Notification
            {
                UserId = winningBid.BidderId,
                Title = "Licitatia castigata",
                Message = $"Ai castigat licitatia {auction.Title}.",
                Type = NotificationType.AuctionWon
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ActivateScheduledAuctionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var scheduled = await db.Auctions
            .Where(x => x.Status == AuctionStatus.Scheduled && x.StartTime <= now && x.EndTime > now)
            .ToListAsync(cancellationToken);

        foreach (var auction in scheduled)
        {
            auction.Status = AuctionStatus.Active;
        }

        if (scheduled.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static IQueryable<Auction> IncludeAuctionDetails(IQueryable<Auction> query)
    {
        return query
            .Include(x => x.Brand)
            .Include(x => x.CarModel)
            .Include(x => x.FuelType)
            .Include(x => x.TransmissionType)
            .Include(x => x.BodyType)
            .Include(x => x.Condition)
            .Include(x => x.DriveType)
            .Include(x => x.Color)
            .Include(x => x.ConditionReport)
            .Include(x => x.Images);
    }
}
