using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.Fluent;
using C3D.Extensions.Aspire.Fluent.Annotations;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class UnderTestExtensions
{
    public static bool IsUnderTest<TResource>(this IResourceBuilder<TResource> resourceBuilder)
        where TResource : IResource
    {
        if (!resourceBuilder.Resource.TryGetLastAnnotation<UnderTestAnnotation>(out var uta))
        {
            uta = new();
            resourceBuilder.WithAnnotation(uta, ResourceAnnotationMutationBehavior.Replace);
        }
        return uta.IsUnderTest;
    }

    public static bool IsUnderTest(this IDistributedApplicationBuilder _) => new StackTrace().ContainsAspireTesting();
    public static bool IsUnderTest(this IHostEnvironment _) => new StackTrace().ContainsAspireTesting();

    public static IResourceBuilder<TResource> WhenUnderTest<TResource>(this IResourceBuilder<TResource> resourceBuilder,
            Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> testAction,
            Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? notTestAction = null
        )
        where TResource : IResource => resourceBuilder.IsUnderTest() ?
                testAction(resourceBuilder) :
                notTestAction?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenNotUnderTest<TResource>(this IResourceBuilder<TResource> resourceBuilder,
            Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> notTestAction,
            Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? testAction = null
        )
        where TResource : IResource => resourceBuilder.IsUnderTest() ?
            testAction?.Invoke(resourceBuilder) ?? resourceBuilder:
            notTestAction(resourceBuilder);

    public static IResourceBuilder<TResource>? WhenUnderTest<TResource>(this IDistributedApplicationBuilder builder,
            Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?> testAction,
            Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? notTestAction = null
        )
        where TResource : IResource => builder.IsUnderTest() ?
            testAction(builder) :
            notTestAction?.Invoke(builder);

    public static IResourceBuilder<TResource>? WhenNotUnderTest<TResource>(this IDistributedApplicationBuilder builder,
            Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?> notTestAction,
            Func<IDistributedApplicationBuilder, IResourceBuilder<TResource>?>? testAction = null
        )
        where TResource : IResource => builder.IsUnderTest() ?
            testAction?.Invoke(builder) :
            notTestAction(builder);
}
