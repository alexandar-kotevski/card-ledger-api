using CardLedger.Application.Abstractions;

namespace CardLedger.Infrastructure.Treasury;

internal sealed class TreasurySyncState : ITreasurySyncState
{
    private readonly TaskCompletionSource _bootstrapCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsBootstrapComplete { get; private set; }

    public Task WaitForBootstrapAsync(CancellationToken cancellationToken = default)
    {
        if (IsBootstrapComplete)
        {
            return Task.CompletedTask;
        }

        return _bootstrapCompletion.Task.WaitAsync(cancellationToken);
    }

    public void MarkBootstrapComplete()
    {
        IsBootstrapComplete = true;
        _bootstrapCompletion.TrySetResult();
    }
}
