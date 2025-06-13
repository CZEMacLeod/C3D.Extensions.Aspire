using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace C3D.Extensions.DependencyInjection.SystemWeb;

public class WebObjectActivator : IServiceProvider
{
    private readonly IServiceProvider serviceProvider;

    public WebObjectActivator(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

    private const BindingFlags flag =
        BindingFlags.Instance | BindingFlags.NonPublic |
        BindingFlags.Public | BindingFlags.CreateInstance;

    public object GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
        {
            return (object)serviceProvider;
        }
        if (serviceType == typeof(IKeyedServiceProvider))
        {
            return serviceProvider as IKeyedServiceProvider ?? serviceProvider.GetService(serviceType);
        }
        return serviceProvider.GetService(serviceType) ??
        Activator.CreateInstance(serviceType, flag, null, null, null);
    }
}
