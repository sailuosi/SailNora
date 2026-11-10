using Hangfire;
using Hangfire.States;
using SailNora.Product.Knowledge.Dtos;
using SailNora.Product.Knowledge.Jobs;

namespace SailNora.App.Scheduling;

/// <summary>Hangfire 开启时入队 <c>document:process</c>（队列 default）。</summary>
public sealed class HangfireDocumentProcessEnqueue : IDocumentProcessEnqueue
{
    public void Enqueue(DocumentProcessPayload payload)
    {
        var client = new BackgroundJobClient();
        client.Create<DocumentProcessJob>(
            job => job.ExecuteAsync(payload, CancellationToken.None),
            new EnqueuedState("default"));
    }
}
