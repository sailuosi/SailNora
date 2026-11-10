using Hangfire;
using Hangfire.States;
using SailNora.Product.Wiki.Dtos;
using SailNora.Product.Wiki.Jobs;

namespace SailNora.App.Scheduling;

/// <summary>Hangfire 开启时入队 <c>wiki:finalize</c>。</summary>
public sealed class HangfireWikiFinalizeEnqueue : IWikiFinalizeEnqueue
{
    public void Enqueue(WikiFinalizePayload payload)
    {
        var client = new BackgroundJobClient();
        client.Create<WikiFinalizeJob>(
            job => job.ExecuteAsync(payload, CancellationToken.None),
            new EnqueuedState("default"));
    }
}
