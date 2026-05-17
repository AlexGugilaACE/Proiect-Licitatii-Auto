using AutoAuction.Domain.Entities;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using AutoAuction.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Tests;

public class TransactionServiceTests
{
    [Fact]
    public async Task ConfirmAsync_confirms_transaction_after_both_parties_confirm()
    {
        await using var db = CreateDbContext();
        var transaction = SeedTransaction(db);
        var service = new TransactionService(db);

        var sellerResult = await service.ConfirmAsync(transaction.Id, "seller-1");
        var buyerResult = await service.ConfirmAsync(transaction.Id, "buyer-1");

        Assert.True(sellerResult);
        Assert.True(buyerResult);
        Assert.True(transaction.SellerConfirmed);
        Assert.True(transaction.BuyerConfirmed);
        Assert.Equal(TransactionStatus.Confirmed, transaction.Status);
        Assert.NotNull(transaction.ConfirmedAt);
        Assert.Equal(2, db.Notifications.Count(x => x.Type == NotificationType.TransactionCreated));
    }

    [Fact]
    public async Task CancelAsync_rejects_confirmed_transaction()
    {
        await using var db = CreateDbContext();
        var transaction = SeedTransaction(db);
        transaction.Status = TransactionStatus.Confirmed;
        await db.SaveChangesAsync();
        var service = new TransactionService(db);

        var result = await service.CancelAsync(transaction.Id, "seller-1");

        Assert.False(result);
        Assert.Equal(TransactionStatus.Confirmed, transaction.Status);
    }

    [Fact]
    public async Task ConfirmAsync_rejects_cancelled_transaction()
    {
        await using var db = CreateDbContext();
        var transaction = SeedTransaction(db);
        transaction.Status = TransactionStatus.Cancelled;
        await db.SaveChangesAsync();
        var service = new TransactionService(db);

        var result = await service.ConfirmAsync(transaction.Id, "buyer-1");

        Assert.False(result);
        Assert.False(transaction.BuyerConfirmed);
        Assert.Equal(TransactionStatus.Cancelled, transaction.Status);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Transaction SeedTransaction(ApplicationDbContext db)
    {
        var transaction = new Transaction
        {
            AuctionId = 1,
            SellerId = "seller-1",
            BuyerId = "buyer-1",
            Amount = 12_000,
            Status = TransactionStatus.Pending
        };

        db.Transactions.Add(transaction);
        db.SaveChanges();
        return transaction;
    }
}
