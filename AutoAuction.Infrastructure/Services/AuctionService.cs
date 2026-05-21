using AutoAuction.Application.DTOs;
using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class AuctionService(ApplicationDbContext db) : IAuctionService
{
    private static readonly TimeSpan BidCooldown = TimeSpan.FromSeconds(5);

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

        if (search.MinMileage is not null)
        {
            query = query.Where(x => x.Mileage >= search.MinMileage.Value);
        }

        if (search.MaxMileage is not null)
        {
            query = query.Where(x => x.Mileage <= search.MaxMileage.Value);
        }

        if (search.MinEngineCapacityCm3 is not null)
        {
            query = query.Where(x => x.EngineCapacityCm3 >= search.MinEngineCapacityCm3.Value);
        }

        if (search.MaxEngineCapacityCm3 is not null)
        {
            query = query.Where(x => x.EngineCapacityCm3 <= search.MaxEngineCapacityCm3.Value);
        }

        if (search.MinHorsePower is not null)
        {
            query = query.Where(x => x.HorsePower >= search.MinHorsePower.Value);
        }

        if (search.MaxHorsePower is not null)
        {
            query = query.Where(x => x.HorsePower <= search.MaxHorsePower.Value);
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

    public async Task<IReadOnlyList<Auction>> GetSimilarAuctionsAsync(Auction auction, int take = 12, CancellationToken cancellationToken = default)
    {
        await ActivateScheduledAuctionsAsync(cancellationToken);

        return await IncludeAuctionDetails(db.Auctions)
            .Where(x => x.Id != auction.Id)
            .Where(x => x.Status == AuctionStatus.Active || x.Status == AuctionStatus.Scheduled)
            .Where(x =>
                x.BodyTypeId == auction.BodyTypeId ||
                x.BrandId == auction.BrandId ||
                x.FuelTypeId == auction.FuelTypeId ||
                x.TransmissionTypeId == auction.TransmissionTypeId)
            .Select(x => new
            {
                Auction = x,
                Score =
                    (x.BodyTypeId == auction.BodyTypeId ? 4 : 0) +
                    (x.BrandId == auction.BrandId ? 3 : 0) +
                    (x.FuelTypeId == auction.FuelTypeId ? 2 : 0) +
                    (x.TransmissionTypeId == auction.TransmissionTypeId ? 1 : 0)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Auction.EndTime)
            .Take(Math.Clamp(take, 1, 24))
            .Select(x => x.Auction)
            .ToListAsync(cancellationToken);
    }

    public async Task<Auction> CreateAsync(string sellerId, AuctionCreateDto dto, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Local).ToUniversalTime();
        var end = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Local).ToUniversalTime();

        var auction = new Auction
        {
            SellerId = sellerId,
            Vin = dto.Vin,
            BrandId = dto.BrandId,
            CarModelId = dto.CarModelId,
            Title = dto.Title,
            Description = dto.Description,
            Year = dto.Year,
            Mileage = dto.Mileage,
            EngineCapacityCm3 = dto.EngineCapacityCm3,
            HorsePower = dto.HorsePower,
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
        var existingCount = await db.AuctionImages.CountAsync(x => x.AuctionId == auctionId, cancellationToken);
        for (var i = 0; i < images.Count; i++)
        {
            db.AuctionImages.Add(new AuctionImage
            {
                AuctionId = auctionId,
                FileName = images[i].FileName,
                FilePath = images[i].FilePath,
                SortOrder = existingCount + i,
                IsMainImage = existingCount == 0 && i == 0
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteImageAsync(int auctionId, int imageId, string sellerId, CancellationToken cancellationToken = default)
    {
        var image = await db.AuctionImages
            .Include(x => x.Auction)
            .FirstOrDefaultAsync(x => x.Id == imageId && x.AuctionId == auctionId && x.Auction!.SellerId == sellerId, cancellationToken);

        if (image is null)
        {
            return false;
        }

        var wasMainImage = image.IsMainImage;
        db.AuctionImages.Remove(image);

        if (wasMainImage)
        {
            var nextMainImage = await db.AuctionImages
                .Where(x => x.AuctionId == auctionId && x.Id != imageId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextMainImage is not null)
            {
                nextMainImage.IsMainImage = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetMainImageAsync(int auctionId, int imageId, string sellerId, CancellationToken cancellationToken = default)
    {
        var auction = await db.Auctions
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == auctionId && x.SellerId == sellerId, cancellationToken);

        if (auction is null || auction.Images.All(x => x.Id != imageId))
        {
            return false;
        }

        foreach (var image in auction.Images)
        {
            image.IsMainImage = image.Id == imageId;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
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
        auction.Vin = dto.Vin;
        auction.Year = dto.Year;
        auction.Mileage = dto.Mileage;
        auction.EngineCapacityCm3 = dto.EngineCapacityCm3;
        auction.HorsePower = dto.HorsePower;
        auction.FuelTypeId = dto.FuelTypeId;
        auction.TransmissionTypeId = dto.TransmissionTypeId;
        auction.BodyTypeId = dto.BodyTypeId;
        auction.ConditionId = dto.ConditionId;
        auction.DriveTypeId = dto.DriveTypeId;
        auction.ColorId = dto.ColorId;
        var hasBids = await db.Bids.AnyAsync(x => x.AuctionId == auctionId, cancellationToken);
        if (!hasBids)
        {
            auction.StartTime = start;
            auction.EndTime = end;
            auction.StartingPrice = dto.StartingPrice;
            auction.CurrentPrice = dto.StartingPrice;
            auction.MinimumBidIncrement = dto.MinimumBidIncrement;
            auction.Status = start <= DateTime.UtcNow ? AuctionStatus.Active : AuctionStatus.Scheduled;
        }

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
        var validationError = ValidateBidAuction(auction, bidderId);
        if (validationError is not null)
        {
            return BidResult.Failure(validationError);
        }
        var activeAuction = auction!;

        var currentTopBid = await db.Bids
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        var cooldownUntil = await GetBidCooldownUntilAsync(auctionId, bidderId, cancellationToken);
        if (cooldownUntil is not null && cooldownUntil > DateTime.UtcNow)
        {
            var seconds = Math.Ceiling((cooldownUntil.Value - DateTime.UtcNow).TotalSeconds);
            return BidResult.Failure($"Te rugam sa astepti {seconds:N0} secunde inainte de urmatoarea oferta.");
        }

        if (currentTopBid?.BidderId == bidderId)
        {
            return BidResult.Failure("Ai deja cea mai mare oferta pentru aceasta licitatie.");
        }

        var minimumAcceptedAmount = activeAuction.CurrentPrice + activeAuction.MinimumBidIncrement;
        if (amount < minimumAcceptedAmount)
        {
            return BidResult.Failure($"Oferta minima este {minimumAcceptedAmount:N2}.");
        }

        var now = DateTime.UtcNow;
        var outbidUserIds = new List<string>();
        AddOutbidUser(outbidUserIds, currentTopBid?.BidderId, bidderId);

        var bid = new Bid
        {
            AuctionId = auctionId,
            BidderId = bidderId,
            Amount = amount,
            CreatedAt = now
        };

        activeAuction.CurrentPrice = amount;
        activeAuction.UpdatedAt = now;
        db.Bids.Add(bid);
        var recentBidTimes = new Dictionary<string, DateTime> { [bidderId] = now };

        db.Notifications.Add(new Notification
        {
            UserId = activeAuction.SellerId,
            Title = "Oferta noua",
            Message = $"Licitatia {activeAuction.Title} a primit o oferta de {amount:N2}.",
            Type = NotificationType.NewBid
        });

        AddOutbidNotification(currentTopBid?.BidderId, bidderId, activeAuction.Title);
        await ProcessNextAutoBidAsync(activeAuction, bidderId, outbidUserIds, recentBidTimes, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return BidResult.Success(activeAuction.CurrentPrice, outbidUserIds, bid.CreatedAt);
    }

    public async Task<BidResult> ConfigureAutoBidAsync(int auctionId, string bidderId, decimal maxAmount, CancellationToken cancellationToken = default)
    {
        var auction = await db.Auctions.FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);
        var validationError = ValidateBidAuction(auction, bidderId);
        if (validationError is not null)
        {
            return BidResult.Failure(validationError);
        }

        var minimumAcceptedAmount = auction!.CurrentPrice + auction.MinimumBidIncrement;
        if (maxAmount < minimumAcceptedAmount)
        {
            return BidResult.Failure($"Autobid-ul maxim trebuie sa fie cel putin {minimumAcceptedAmount:N2}.");
        }

        var now = DateTime.UtcNow;
        var autoBid = await db.AutoBids
            .FirstOrDefaultAsync(x => x.AuctionId == auctionId && x.BidderId == bidderId, cancellationToken);

        if (autoBid is null)
        {
            db.AutoBids.Add(new AutoBid
            {
                AuctionId = auctionId,
                BidderId = bidderId,
                MaxAmount = maxAmount,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            autoBid.MaxAmount = maxAmount;
            autoBid.IsActive = true;
            autoBid.UpdatedAt = now;
        }

        var outbidUserIds = new List<string>();
        DateTime? bidCreatedAt = null;
        string? currentTopBidderId;
        var recentBidTimes = new Dictionary<string, DateTime>();
        var currentTopBid = await db.Bids
            .Where(x => x.AuctionId == auctionId)
            .OrderByDescending(x => x.Amount)
            .FirstOrDefaultAsync(cancellationToken);
        currentTopBidderId = currentTopBid?.BidderId;

        if (currentTopBid?.BidderId != bidderId)
        {
            var cooldownUntil = await GetBidCooldownUntilAsync(auctionId, bidderId, cancellationToken);
            if (cooldownUntil is not null && cooldownUntil > now)
            {
                await db.SaveChangesAsync(cancellationToken);
                return BidResult.Success(auction.CurrentPrice, outbidUserIds);
            }

            var initialAmount = Math.Min(maxAmount, minimumAcceptedAmount);
            AddOutbidUser(outbidUserIds, currentTopBid?.BidderId, bidderId);

            db.Bids.Add(new Bid
            {
                AuctionId = auctionId,
                BidderId = bidderId,
                Amount = initialAmount,
                CreatedAt = now
            });
            bidCreatedAt = now;
            recentBidTimes[bidderId] = now;
            currentTopBidderId = bidderId;

            auction.CurrentPrice = initialAmount;
            auction.UpdatedAt = now;
            db.Notifications.Add(new Notification
            {
                UserId = auction.SellerId,
                Title = "Oferta noua",
                Message = $"Licitatia {auction.Title} a primit o oferta automata de {initialAmount:N2}.",
                Type = NotificationType.NewBid
            });
            AddOutbidNotification(currentTopBid?.BidderId, bidderId, auction.Title);
        }

        await ProcessNextAutoBidAsync(auction, currentTopBidderId, outbidUserIds, recentBidTimes, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return BidResult.Success(auction.CurrentPrice, outbidUserIds, bidCreatedAt);
    }

    public async Task<decimal?> GetAutoBidMaxAmountAsync(int auctionId, string bidderId, CancellationToken cancellationToken = default)
    {
        return await db.AutoBids
            .Where(x => x.AuctionId == auctionId && x.BidderId == bidderId && x.IsActive)
            .Select(x => (decimal?)x.MaxAmount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DisableAutoBidAsync(int auctionId, string bidderId, CancellationToken cancellationToken = default)
    {
        var autoBid = await db.AutoBids
            .FirstOrDefaultAsync(x => x.AuctionId == auctionId && x.BidderId == bidderId && x.IsActive, cancellationToken);

        if (autoBid is null)
        {
            return false;
        }

        autoBid.IsActive = false;
        autoBid.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DateTime?> GetBidCooldownUntilAsync(int auctionId, string bidderId, CancellationToken cancellationToken = default)
    {
        var lastBidAt = await db.Bids
            .Where(x => x.AuctionId == auctionId && x.BidderId == bidderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (DateTime?)x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return lastBidAt?.Add(BidCooldown);
    }

    public async Task<IReadOnlyList<AutoBidProcessingResult>> ProcessPendingAutoBidsAsync(CancellationToken cancellationToken = default)
    {
        await ActivateScheduledAuctionsAsync(cancellationToken);

        var auctionIds = await db.AutoBids
            .Where(x => x.IsActive)
            .Select(x => x.AuctionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var results = new List<AutoBidProcessingResult>();

        foreach (var auctionId in auctionIds)
        {
            var auction = await db.Auctions.FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);
            if (auction is null || auction.Status != AuctionStatus.Active || auction.EndTime <= DateTime.UtcNow)
            {
                continue;
            }

            var currentTopBid = await db.Bids
                .Where(x => x.AuctionId == auction.Id)
                .OrderByDescending(x => x.Amount)
                .FirstOrDefaultAsync(cancellationToken);
            if (currentTopBid is null)
            {
                continue;
            }

            var outbidUserIds = new List<string>();
            var recentBidTimes = new Dictionary<string, DateTime>();
            var bidCreatedAt = await ProcessNextAutoBidAsync(auction, currentTopBid.BidderId, outbidUserIds, recentBidTimes, cancellationToken);
            if (bidCreatedAt is not null)
            {
                results.Add(new AutoBidProcessingResult
                {
                    AuctionId = auction.Id,
                    CurrentPrice = auction.CurrentPrice,
                    OutbidUserIds = outbidUserIds,
                    BidCreatedAt = bidCreatedAt.Value
                });
            }
        }

        if (results.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return results;
    }

    public async Task<bool> ForceCloseAuctionAsync(int auctionId, string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var auction = await db.Auctions
            .Include(x => x.Bids)
            .FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);

        if (auction is null ||
            string.IsNullOrWhiteSpace(userId) ||
            auction.Status is AuctionStatus.Ended or AuctionStatus.Unsold or AuctionStatus.Cancelled)
        {
            return false;
        }

        var winningBid = auction.Bids.OrderByDescending(x => x.Amount).FirstOrDefault();
        auction.EndTime = DateTime.UtcNow;

        if (winningBid is null)
        {
            auction.Status = AuctionStatus.Unsold;
            db.Notifications.Add(new Notification
            {
                UserId = auction.SellerId,
                Title = "Licitatie fara castigator",
                Message = $"Licitatia {auction.Title} s-a incheiat fara oferte.",
                Type = NotificationType.AuctionEnded
            });
        }
        else
        {
            auction.Status = AuctionStatus.Ended;
            auction.WinningBidId = winningBid.Id;

            if (!await db.Transactions.AnyAsync(x => x.AuctionId == auction.Id, cancellationToken))
            {
                db.Transactions.Add(new Domain.Entities.Transaction
                {
                    AuctionId = auction.Id,
                    SellerId = auction.SellerId,
                    BuyerId = winningBid.BidderId,
                    Amount = winningBid.Amount
                });
            }

            db.Notifications.Add(new Notification
            {
                UserId = winningBid.BidderId,
                Title = "Licitatia castigata",
                Message = $"Ai castigat licitatia {auction.Title}.",
                Type = NotificationType.AuctionWon
            });

            db.Notifications.Add(new Notification
            {
                UserId = auction.SellerId,
                Title = "Licitatie finalizata",
                Message = $"Licitatia {auction.Title} a fost castigata cu {winningBid.Amount:N2}.",
                Type = NotificationType.AuctionEnded
            });

            var losingBidderIds = auction.Bids
                .Where(x => x.BidderId != winningBid.BidderId)
                .Select(x => x.BidderId)
                .Distinct()
                .ToList();

            foreach (var bidderId in losingBidderIds)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = bidderId,
                    Title = "Licitatie pierduta",
                    Message = $"Licitatia {auction.Title} s-a incheiat. Oferta castigatoare a fost {winningBid.Amount:N2}.",
                    Type = NotificationType.AuctionLost
                });
            }
        }

        var activeAutoBids = await db.AutoBids
            .Where(x => x.AuctionId == auctionId && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var autoBid in activeAutoBids)
        {
            autoBid.IsActive = false;
            autoBid.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
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
                db.Notifications.Add(new Notification
                {
                    UserId = auction.SellerId,
                    Title = "Licitație fără câștigător",
                    Message = $"Licitația {auction.Title} s-a încheiat fără oferte.",
                    Type = NotificationType.AuctionEnded
                });
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

            db.Notifications.Add(new Notification
            {
                UserId = auction.SellerId,
                Title = "Licitație finalizată",
                Message = $"Licitația {auction.Title} a fost câștigată cu {winningBid.Amount:N2}.",
                Type = NotificationType.AuctionEnded
            });

            var losingBidderIds = auction.Bids
                .Where(x => x.BidderId != winningBid.BidderId)
                .Select(x => x.BidderId)
                .Distinct()
                .ToList();

            foreach (var bidderId in losingBidderIds)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = bidderId,
                    Title = "Licitație pierdută",
                    Message = $"Licitația {auction.Title} s-a încheiat. Oferta câștigătoare a fost {winningBid.Amount:N2}.",
                    Type = NotificationType.AuctionLost
                });
            }
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

    private static string? ValidateBidAuction(Auction? auction, string bidderId)
    {
        if (auction is null)
        {
            return "Licitatia nu exista.";
        }

        var now = DateTime.UtcNow;
        if (auction.SellerId == bidderId)
        {
            return "Nu poti licita la propria licitatie.";
        }

        if (auction.Status == AuctionStatus.Scheduled && auction.StartTime <= now)
        {
            auction.Status = AuctionStatus.Active;
        }

        if (auction.Status != AuctionStatus.Active || auction.StartTime > now || auction.EndTime <= now)
        {
            return "Licitatia nu este activa.";
        }

        return null;
    }

    private async Task<DateTime?> ProcessNextAutoBidAsync(
        Auction auction,
        string? currentTopBidderId,
        List<string> outbidUserIds,
        Dictionary<string, DateTime> recentBidTimes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentTopBidderId))
        {
            return null;
        }

        var nextAmount = auction.CurrentPrice + auction.MinimumBidIncrement;
        var autoBidCandidates = await db.AutoBids
            .Where(x => x.AuctionId == auction.Id && x.IsActive)
            .Where(x => x.BidderId != currentTopBidderId)
            .Where(x => x.MaxAmount >= nextAmount)
            .OrderByDescending(x => x.MaxAmount)
            .ThenBy(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);

        if (autoBidCandidates.Count == 0)
        {
            var exhaustedAutoBids = await db.AutoBids
                .Where(x => x.AuctionId == auction.Id && x.IsActive && x.MaxAmount < nextAmount)
                .ToListAsync(cancellationToken);

            foreach (var exhaustedAutoBid in exhaustedAutoBids)
            {
                exhaustedAutoBid.IsActive = false;
            }

            return null;
        }

        var now = DateTime.UtcNow;
        AutoBid? autoBid = null;
        foreach (var candidate in autoBidCandidates)
        {
            var candidateCooldownUntil = await GetBidCooldownUntilAsync(auction.Id, candidate.BidderId, recentBidTimes, cancellationToken);
            if (candidateCooldownUntil is null || candidateCooldownUntil <= now)
            {
                autoBid = candidate;
                break;
            }
        }

        if (autoBid is null)
        {
            return null;
        }

        db.Bids.Add(new Bid
        {
            AuctionId = auction.Id,
            BidderId = autoBid.BidderId,
            Amount = nextAmount,
            CreatedAt = now
        });

        recentBidTimes[autoBid.BidderId] = now;
        auction.CurrentPrice = nextAmount;
        auction.UpdatedAt = now;
        AddOutbidUser(outbidUserIds, currentTopBidderId, autoBid.BidderId);
        AddOutbidNotification(currentTopBidderId, autoBid.BidderId, auction.Title);

        db.Notifications.Add(new Notification
        {
            UserId = auction.SellerId,
            Title = "Oferta automata",
            Message = $"Licitatia {auction.Title} a primit un autobid de {nextAmount:N2}.",
            Type = NotificationType.NewBid
        });

        return now;
    }

    private async Task<DateTime?> GetBidCooldownUntilAsync(
        int auctionId,
        string bidderId,
        IReadOnlyDictionary<string, DateTime> recentBidTimes,
        CancellationToken cancellationToken)
    {
        return recentBidTimes.TryGetValue(bidderId, out var recentBidAt)
            ? recentBidAt.Add(BidCooldown)
            : await GetBidCooldownUntilAsync(auctionId, bidderId, cancellationToken);
    }

    private static void AddOutbidUser(List<string> outbidUserIds, string? outbidUserId, string newBidderId)
    {
        if (!string.IsNullOrWhiteSpace(outbidUserId) &&
            outbidUserId != newBidderId &&
            !outbidUserIds.Contains(outbidUserId))
        {
            outbidUserIds.Add(outbidUserId);
        }
    }

    private void AddOutbidNotification(string? outbidUserId, string newBidderId, string auctionTitle)
    {
        if (string.IsNullOrWhiteSpace(outbidUserId) || outbidUserId == newBidderId)
        {
            return;
        }

        db.Notifications.Add(new Notification
        {
            UserId = outbidUserId,
            Title = "Ai fost depasit",
            Message = $"A fost plasata o oferta mai mare pentru {auctionTitle}.",
            Type = NotificationType.Outbid
        });
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
            .Include(x => x.Images)
            .Include(x => x.WinningBid);
    }
}
