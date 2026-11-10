using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace SailNora.Foundation.DI;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddSailNoraServiceDiscovery(
        this IServiceCollection services,
        params Type[] assemblyAnchorTypes)
    {
        if (assemblyAnchorTypes.Length == 0)
            throw new ArgumentException("至少需要一个程序集锚点类型。", nameof(assemblyAnchorTypes));

        RegisterMarked<ISingleton>(services, assemblyAnchorTypes, ServiceLifetime.Singleton);
        RegisterMarked<IScoped>(services, assemblyAnchorTypes, ServiceLifetime.Scoped);
        RegisterMarked<ITransient>(services, assemblyAnchorTypes, ServiceLifetime.Transient);
        return services;
    }

    private static void RegisterMarked<TMarker>(
        IServiceCollection services,
        Type[] assemblyAnchorTypes,
        ServiceLifetime lifetime)
    {
        services.Scan(scan => scan
            .FromAssembliesOf(assemblyAnchorTypes)
            .AddClasses(classes => classes.AssignableTo<TMarker>()
                .Where(t => t is { IsClass: true, IsAbstract: false }))
            .UsingRegistrationStrategy(RegistrationStrategy.Skip)
            .AsSelf()
            .AsImplementedInterfaces(IsInfrastructureInterface)
            .WithLifetime(lifetime));
    }

    private static bool IsInfrastructureInterface(Type interfaceType)
    {
        if (!interfaceType.IsInterface)
            return false;

        if (interfaceType == typeof(ISingleton)
            || interfaceType == typeof(IScoped)
            || interfaceType == typeof(ITransient))
            return false;

        if (!interfaceType.Name.StartsWith('I'))
            return false;

        return !interfaceType.Name.EndsWith("Service", StringComparison.Ordinal);
    }
}
