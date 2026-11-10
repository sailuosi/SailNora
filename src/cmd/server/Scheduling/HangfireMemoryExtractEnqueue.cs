using Hangfire;
using Hangfire.States;
using SailNora.Product.Memory.Dtos;
using SailNora.Product.Memory.Jobs;

namespace SailNora.App.Scheduling;

public sealed class HangfireMemoryExtractEnqueue : IMemoryExtractEnqueue
{
    public void Enqueue(MemoryExtractPayload payload)
    {
        var client = new BackgroundJobClient();
        client.Create<MemoryExtractJob>(
            job => job.ExecuteAsync(payload, CancellationToken.None),
            new EnqueuedState("default"));
    }
}
