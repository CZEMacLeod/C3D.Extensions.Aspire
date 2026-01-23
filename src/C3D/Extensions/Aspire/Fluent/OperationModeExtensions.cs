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

    public static IResourceBuilder<TResource>? WhenOperationMode<TResource>(this IDistributedApplicationBuilder builder,
        DistributedApplicationOperation mode,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?> matchedMode,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? otherModes)
        where TResource : IResource => builder.ExecutionContext.Operation == mode ?
            matchedMode(builder) :
            otherModes?.Invoke(builder);

    public static IResourceBuilder<TResource>? WhenOperationMode<TResource>(this IDistributedApplicationBuilder builder,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? runMode,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? publishMode,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? otherModes)
        where TResource : IResource => builder.ExecutionContext.Operation switch
        {
            DistributedApplicationOperation.Run => runMode?.Invoke(builder),
            DistributedApplicationOperation.Publish => publishMode?.Invoke(builder),
            _ => otherModes?.Invoke(builder)
        };

    public static IResourceBuilder<TResource>? WhenRunMode<TResource>(this IDistributedApplicationBuilder builder,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?> runMode,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? otherModes)
        where TResource : IResource => builder.ExecutionContext.IsRunMode ?
            runMode(builder) :
            otherModes?.Invoke(builder);

    public static IResourceBuilder<TResource>? WhenPublishMode<TResource>(this IDistributedApplicationBuilder builder,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?> publishMode,
        Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? otherModes)
        where TResource : IResource => builder.ExecutionContext.IsPublishMode ?
            publishMode(builder) :
            otherModes?.Invoke(builder);

}
