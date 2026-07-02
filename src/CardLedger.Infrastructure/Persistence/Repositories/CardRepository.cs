using CardLedger.Application.Abstractions;
using CardLedger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository : ICardRepository
{
    private readonly CardLedgerDbContext _dbContext;

    public CardRepository(CardLedgerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Card?> GetByPanAsync(string pan, CancellationToken cancellationToken = default) =>
        await _dbContext.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Pan == pan, cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> ExistsByPanAsync(string pan, CancellationToken cancellationToken = default) =>
        await _dbContext.Cards
            .AnyAsync(x => x.Pan == pan, cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Card card, CancellationToken cancellationToken = default)
    {
        await _dbContext.Cards.AddAsync(card, cancellationToken).ConfigureAwait(false);
    }
}
