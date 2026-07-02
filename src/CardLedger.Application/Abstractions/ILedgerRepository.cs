using CardLedger.Domain.Entities;

namespace CardLedger.Application.Abstractions;

public interface ILedgerRepository
{
    Task<Ledger?> GetByCardIdAsync(Guid cardId, CancellationToken cancellationToken = default);

    Task<Ledger?> GetByCardPanAsync(string pan, CancellationToken cancellationToken = default);

    Task AddAsync(Ledger ledger, CancellationToken cancellationToken = default);

    void Update(Ledger ledger);
}
