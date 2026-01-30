using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Hosting;

public static class PortAllocatorExtensions
{
    /// <summary>
    /// Add an IPortAllocator service based on actually free host ports
    /// </summary>
    /// <param name="services">An IServiceCollection</param>
    /// <param name="minPort">The minimum port number to allocate (Defaults to 8000)</param>
    /// <returns>The IServiceCollection</returns>
    public static IServiceCollection AddHostPortAllocator(this IServiceCollection services, int minPort = 8000)
    {
        services.TryAddSingleton<C3D.Extensions.Networking.PortAllocator>();
        services.AddSingleton<IPortAllocator>(sp => ActivatorUtilities.CreateInstance<HostPortAllocator>(sp, minPort));
        return services;
    }

    /// <summary>
    /// Add an IPortAllocator service based on actually free host ports with a key.
    /// </summary>
    /// <param name="services">An IServiceCollection</param>
    /// <param name="key">The key to use when getting the keyed service</param>
    /// <param name="minPort">The minimum port number to allocate (Defaults to 8000)</param>
    /// <returns>The IServiceCollection</returns>
    /// <remarks>The actual port allocator service is shared across all instances. The key only affects the min port number allocated.</remarks>
    public static IServiceCollection AddKeyedHostPortAllocator(this IServiceCollection services, object key, int minPort = 8000)
    {
        services.TryAddSingleton<C3D.Extensions.Networking.PortAllocator>();
        services.AddKeyedSingleton<IPortAllocator>(key, (sp, _) => ActivatorUtilities.CreateInstance<HostPortAllocator>(sp, minPort));
        return services;
    }

}
