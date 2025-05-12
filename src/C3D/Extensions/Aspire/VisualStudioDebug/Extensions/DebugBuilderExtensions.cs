using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using C3D.Extensions.Aspire.VisualStudioDebug.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DebugBuilderExtensions
{
    public static IDebugBuilder<TResource> WithDebugEngine<TResource>(this IDebugBuilder<TResource> debugBuilder,
        string engine)
        where TResource : IResource
    {
        if (!debugBuilder.IsDebugMode)
        {
            return debugBuilder;
        }
        debugBuilder.ResourceBuilder.WithAnnotation<DebugAttachEngineAnnotation>(new() { Engine = engine });
        return debugBuilder;
    }

    public static IDebugBuilder<TResource> WithDebugSkip<TResource>(this IDebugBuilder<TResource> debugBuilder,
        bool skip = true)
        where TResource : IResource
    {
        if (!debugBuilder.IsDebugMode)
        {
            return debugBuilder;
        }
        if (debugBuilder.ResourceBuilder.Resource.TryGetLastAnnotation<DebugAttachAnnotation>(out var annotation))
        {
            annotation.Skip = skip;
        }
        return debugBuilder;
    }

    public static IDebugBuilder<TResource> WithDebugEngines<TResource>(this IDebugBuilder<TResource> debugBuilder,
        params string[] engines)
    where TResource : IResource
    {
        if (!debugBuilder.IsDebugMode)
        {
            return debugBuilder;
        }
        if (engines is not null)
        {
            foreach (var engine in engines)
            {
                debugBuilder.ResourceBuilder.WithAnnotation<DebugAttachEngineAnnotation>(new() { Engine = engine });
            }
        }
        return debugBuilder;
    }

    public static IDebugBuilder<TResource> WithDebugTransport<TResource>(this IDebugBuilder<TResource> debugBuilder,
        string transport, string? qualifier = null)
        where TResource : IResource
    {
        if (!debugBuilder.IsDebugMode)
        {
            return debugBuilder;
        }
        debugBuilder.ResourceBuilder.WithAnnotation<DebugAttachTransportAnnotation>(new() { Transport = transport, Qualifier = qualifier });
        return debugBuilder;
    }

    public static IDebugBuilder<TResource> WithDebuggerHealthcheck<TResource>(this IDebugBuilder<TResource> debugBuilder)
        where TResource : IResource
    {
        if (!debugBuilder.IsDebugMode)
        {
            return debugBuilder;
        }

        var healthCheckKey = $"{debugBuilder.ResourceBuilder.Resource.Name}_debugger_check";
        if (debugBuilder.ResourceBuilder.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations)
            && annotations.Any(a => a.Key == healthCheckKey))
        {
            return debugBuilder;
        }

        debugBuilder.ResourceBuilder.ApplicationBuilder.Services.AddHealthChecks()
            .AddTypeActivatedCheck<DebugHealthCheck>(healthCheckKey, debugBuilder.ResourceBuilder.Resource.Name);

        debugBuilder.ResourceBuilder.WithHealthCheck(healthCheckKey);

        return debugBuilder;
    }
}
