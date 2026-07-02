namespace CardLedger.Application.Abstractions;

public interface ITreasurySyncState
{
    bool IsBootstrapComplete { get; }

    Task WaitForBootstrapAsync(CancellationToken cancellationToken = default);
}
