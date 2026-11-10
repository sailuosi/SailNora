using Hangfire;
using Hangfire.States;
using SailNora.Product.Datasource.Dtos;
using SailNora.Product.Datasource.Jobs;

namespace SailNora.App.Scheduling;

public sealed class HangfireDatasourceSyncEnqueue : IDatasourceSyncEnqueue
{
    public void Enqueue(DatasourceSyncPayload payload)
    {
        var client = new BackgroundJobClient();
        client.Create<DatasourceSyncJob>(
            job => job.ExecuteAsync(payload, CancellationToken.None),
            new EnqueuedState("default"));
    }
}
