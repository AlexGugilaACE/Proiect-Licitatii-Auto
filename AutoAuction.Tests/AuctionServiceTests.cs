using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Tests;

public class AuctionServiceTests
{
    [Fact]
    public async Task PlaceBidAsync_rejects_seller_bid()
    {
        await using var db = CreateDbContext();
        var auction = SeedAuction(db, sellerId: "seller-1");
        var service = new AuctionService(db);

        var result = await service.PlaceBidAsync(auction.Id, "seller-1", 12_000);

        Assert.False(result.Succeeded);
        Assert.Equal(10_000, auction.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBidAsync_rejects_bid_lower_than_current_price()
    {
        await using var db = CreateDbContext();
        var auction = SeedAuction(db, sellerId: "seller-1");
        var service = new AuctionService(db);

        var result = await service.PlaceBidAsync(auction.Id, "buyer-1", 10_050);

        Assert.False(result.Succeeded);
        Assert.Equal(10_000, auction.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBidAsync_rejects_bid_from_current_top_bidder()
    {
        await using var db = CreateDbContext();
        var auction = SeedAuction(db, sellerId: "seller-1");
        db.Bids.Add(new Bid
        {
            AuctionId = auction.Id,
            BidderId = "buyer-1",
            Amount = 11_000
        });
        auction.CurrentPrice = 11_000;
        await db.SaveChangesAsync();

        var service = new AuctionService(db);

        var result = await service.PlaceBidAsync(auction.Id, "buyer-1", 12_000);

        Assert.False(result.Succeeded);
        Assert.Equal(11_000, auction.CurrentPrice);
    }

    [Fact]
    public async Task PlaceBidAsync_accepts_valid_bid_and_updates_current_price()
    {
        await using var db = CreateDbContext();
        var auction = SeedAuction(db, sellerId: "seller-1");
        var service = new AuctionService(db);

        var result = await service.PlaceBidAsync(auction.Id, "buyer-1", 12_000);

        Assert.True(result.Succeeded);
        Assert.Equal(12_000, result.CurrentPrice);
        Assert.Equal(12_000, auction.CurrentPrice);
        Assert.Single(db.Bids);
    }

    [Fact]
    public async Task CloseExpiredAuctionsAsync_marks_auction_without_bids_as_unsold()
    {
        await using var db = CreateDbContext();
        var auction = SeedAuction(db, sellerId: "seller-1");
        auction.EndTime = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        var service = new AuctionService(db);

        await service.CloseExpiredAuctionsAsync();

        Assert.Equal(AuctionStatus.Unsold, auction.Status);
        Assert.Null(auction.WinningBidId);
        Assert.Empty(db.Transactions);
        Assert.Contains(db.Notifications, x => x.UserId == "seller-1" && x.Type == NotificationType.AuctionEnded);
    }

    [Fact]
    public async Task CloseExpiredAuctionsAsync_sets_winner_creates_transaction_and_notifications()
    {
        await using var db = CreateDbContext();
        var auction = SeedAuction(db, sellerId: "seller-1");
        auction.EndTime = DateTime.UtcNow.AddMinutes(-1);
        db.Bids.AddRange(
            new Bid { AuctionId = auction.Id, BidderId = "buyer-1", Amount = 11_000 },
            new Bid { AuctionId = auction.Id, BidderId = "buyer-2", Amount = 12_000 });
        await db.SaveChangesAsync();
        var service = new AuctionService(db);

        await service.CloseExpiredAuctionsAsync();

        Assert.Equal(AuctionStatus.Ended, auction.Status);
        Assert.NotNull(auction.WinningBidId);
        Assert.Single(db.Transactions);
        Assert.Contains(db.Notifications, x => x.UserId == "buyer-2" && x.Type == NotificationType.AuctionWon);
        Assert.Contains(db.Notifications, x => x.UserId == "buyer-1" && x.Type == NotificationType.AuctionLost);
        Assert.Contains(db.Notifications, x => x.UserId == "seller-1" && x.Type == NotificationType.AuctionEnded);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Auction SeedAuction(ApplicationDbContext db, string sellerId)
    {
        var auction = new Auction
        {
            SellerId = sellerId,
            Title = "BMW Seria 3",
            Description = "Test",
            Vin = "WBA8E11020K123456",
            BrandId = 1,
            CarModelId = 1,
            Year = 2020,
            Mileage = 100_000,
            EngineCapacityCm3 = 1995,
            HorsePower = 190,
            FuelTypeId = 1,
            TransmissionTypeId = 2,
            BodyTypeId = 3,
            ConditionId = 4,
            DriveTypeId = 5,
            ColorId = 6,
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow.AddHours(1),
            StartingPrice = 10_000,
            CurrentPrice = 10_000,
            MinimumBidIncrement = 100,
            Status = AuctionStatus.Active
        };

        db.Auctions.Add(auction);
        db.SaveChanges();
        return auction;
    }
}
