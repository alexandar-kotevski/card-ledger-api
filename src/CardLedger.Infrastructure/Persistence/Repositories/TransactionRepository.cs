using CardLedger.Application.Abstractions;
using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly CardLedgerDbContext _dbContext;

    public TransactionRepository(CardLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Transaction>> GetByCardPanAsync(
        string pan,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Card!.Pan == pan)
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<Transaction?> GetByCardPanAndIdAsync(
        string pan,
        Guid transactionId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Card!.Pan == pan && x.Id == transactionId,
                cancellationToken)
            .ConfigureAwait(false);
}
