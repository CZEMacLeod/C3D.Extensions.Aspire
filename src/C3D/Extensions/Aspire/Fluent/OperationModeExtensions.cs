using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class OperationModeExtensions
{
    public static IResourceBuilder<TResource> WhenOperationMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        DistributedApplicationOperation mode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> matchedMode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherModes)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.ExecutionContext.Operation == mode ?
            matchedMode(resourceBuilder) :
            otherModes?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenOperationMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? runMode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? publishMode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherModes)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.ExecutionContext.Operation switch
        {
            DistributedApplicationOperation.Run => runMode?.Invoke(resourceBuilder) ?? resourceBuilder,
            DistributedApplicationOperation.Publish => publishMode?.Invoke(resourceBuilder) ?? resourceBuilder,
            _ => otherModes?.Invoke(resourceBuilder) ?? resourceBuilder
        };

    public static IResourceBuilder<TResource> WhenRunMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> runMode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherModes)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode ?
            runMode(resourceBuilder) :
            otherModes?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenPublishMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> publishMode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherModes)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.ExecutionContext.IsPublishMode ?
            publishMode(resourceBuilder) :
            otherModes?.Invoke(resourceBuilder) ?? resourceBuilder;

    [Experimental("ASPIREPUBLISHERS001")]
    public static IResourceBuilder<TResource> WhenInspectMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> inspectMode,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherModes)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.ExecutionContext.IsInspectMode ?
            inspectMode(resourceBuilder) :
            otherModes?.Invoke(resourceBuilder) ?? resourceBuilder;
}
