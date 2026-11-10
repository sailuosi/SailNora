using Hangfire;
using Hangfire.States;
using SailNora.Product.Wiki.Dtos;
using SailNora.Product.Wiki.Jobs;

namespace SailNora.App.Scheduling;

/// <summary>Hangfire 开启时入队 <c>wiki:ingest</c>。</summary>
public sealed class HangfireWikiIngestEnqueue : IWikiIngestEnqueue
{
    public void Enqueue(WikiIngestPayload payload)
    {
        var client = new BackgroundJobClient();
        client.Create<WikiIngestJob>(
            job => job.ExecuteAsync(payload, CancellationToken.None),
            new EnqueuedState("default"));
    }
}
