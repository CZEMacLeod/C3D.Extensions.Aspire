using Aspire.Hosting.ApplicationModel;
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

    public static bool ContainsAspireTesting(this StackTrace callStack) =>
        callStack.GetStackFrames().Any(sf =>
            sf.GetMethod()?.DeclaringType?.Assembly.GetName().Name == "Aspire.Hosting.Testing");

    private static StackFrame[] GetStackFrames(this StackTrace callStack) => callStack.GetFrames() ?? [];
}
