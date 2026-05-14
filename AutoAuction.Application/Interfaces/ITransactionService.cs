using AutoAuction.Domain.Entities;

namespace AutoAuction.Application.Interfaces;

public interface ITransactionService
{
    Task<IReadOnlyList<Transaction>> GetUserTransactionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ConfirmAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(int id, string userId, CancellationToken cancellationToken = default);
}
