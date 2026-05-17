using AutoAuction.Application.Interfaces;
using AutoAuction.Domain.Enums;
using AutoAuction.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoAuction.Infrastructure.Services;

public class TransactionService(ApplicationDbContext db) : ITransactionService
{
    public async Task<IReadOnlyList<Domain.Entities.Transaction>> GetUserTransactionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await db.Transactions
            .Include(x => x.Auction)
                .ThenInclude(x => x!.Brand)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.CarModel)
            .Where(x => x.SellerId == userId || x.BuyerId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Domain.Entities.Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await db.Transactions
            .Include(x => x.Auction)
                .ThenInclude(x => x!.Brand)
            .Include(x => x.Auction)
                .ThenInclude(x => x!.CarModel)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ConfirmAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transaction is null || !CanUpdate(transaction, userId))
        {
            return false;
        }

        if (transaction.SellerId == userId)
        {
            transaction.SellerConfirmed = true;
        }

        if (transaction.BuyerId == userId)
        {
            transaction.BuyerConfirmed = true;
        }

        if (transaction.SellerConfirmed && transaction.BuyerConfirmed)
        {
            transaction.Status = TransactionStatus.Confirmed;
            transaction.ConfirmedAt = DateTime.UtcNow;
            db.Notifications.Add(new Domain.Entities.Notification
            {
                UserId = transaction.SellerId,
                Title = "Tranzactie confirmata",
                Message = "Tranzactia a fost confirmata de ambele parti.",
                Type = NotificationType.TransactionCreated
            });
            db.Notifications.Add(new Domain.Entities.Notification
            {
                UserId = transaction.BuyerId,
                Title = "Tranzactie confirmata",
                Message = "Tranzactia a fost confirmata de ambele parti.",
                Type = NotificationType.TransactionCreated
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transaction is null || !CanUpdate(transaction, userId))
        {
            return false;
        }

        transaction.Status = TransactionStatus.Cancelled;
        db.Notifications.Add(new Domain.Entities.Notification
        {
            UserId = transaction.SellerId == userId ? transaction.BuyerId : transaction.SellerId,
            Title = "Tranzactie anulata",
            Message = "Tranzactia a fost anulata.",
            Type = NotificationType.TransactionCreated
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool CanUpdate(Domain.Entities.Transaction transaction, string userId)
    {
        return transaction.Status == TransactionStatus.Pending
            && (transaction.SellerId == userId || transaction.BuyerId == userId);
    }
}
