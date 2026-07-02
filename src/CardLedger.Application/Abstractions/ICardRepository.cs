using CardLedger.Domain.Entities;

namespace CardLedger.Application.Abstractions;

public interface ICardRepository
{
    Task<Card?> GetByPanAsync(string pan, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPanAsync(string pan, CancellationToken cancellationToken = default);

    Task AddAsync(Card card, CancellationToken cancellationToken = default);
}
