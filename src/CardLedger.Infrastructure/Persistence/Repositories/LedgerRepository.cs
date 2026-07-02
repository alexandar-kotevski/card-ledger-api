using CardLedger.Application.Abstractions;
using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Infrastructure.Persistence.Repositories;

internal sealed class LedgerRepository : ILedgerRepository
{
    private readonly CardLedgerDbContext _dbContext;

    public LedgerRepository(CardLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Ledger?> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default) =>
        await _dbContext.Ledgers
            .FirstOrDefaultAsync(x => x.CardId == cardId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<Ledger?> GetByCardPanAsync(string pan, CancellationToken cancellationToken = default) =>
        await _dbContext.Ledgers
            .AsNoTracking()
            .Include(x => x.Card)
            .FirstOrDefaultAsync(x => x.Card!.Pan == pan, cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Ledger ledger, CancellationToken cancellationToken = default)
    {
        await _dbContext.Ledgers.AddAsync(ledger, cancellationToken).ConfigureAwait(false);
    }

    public void Update(Ledger ledger) => _dbContext.Ledgers.Update(ledger);
}
