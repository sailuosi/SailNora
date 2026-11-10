using Microsoft.Extensions.DependencyInjection;
using SailNora.Product.Memory.Jobs;

namespace SailNora.Product.Memory;

public static class MemorySetup
{
    public static IServiceCollection AddSailNoraMemory(this IServiceCollection services)
    {
        services.AddScoped<IMemoryExtractEnqueue, InlineMemoryExtractEnqueue>();
        return services;
    }
}
