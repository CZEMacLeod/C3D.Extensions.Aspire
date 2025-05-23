﻿using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.VisualStudioDebug;
using C3D.Extensions.Aspire.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DebuggerServicesExtensions
{
    public static IServiceCollection AddAttachDebuggerHook(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindows() && !(new StackTrace().ContainsAspireTesting()))
        {
            services
                .InsertHostedService<AttachDebuggerHook>()
                .AddOptions<DebuggerHookOptions>()
                .BindConfiguration("DebuggerHook");
            services.TryAddSingleton<VisualStudioInstances>();
        }
        return services;

    }
}
