using CardLedger.Domain.Entities;

namespace CardLedger.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetByCardPanAsync(string pan, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByCardPanAndIdAsync(string pan, Guid transactionId, CancellationToken cancellationToken = default);
}
