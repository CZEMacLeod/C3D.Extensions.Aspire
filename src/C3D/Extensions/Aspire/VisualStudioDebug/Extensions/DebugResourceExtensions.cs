using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using Microsoft.Extensions.Hosting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DebugResourceBuilderExtensions
{
    public static bool IsDebugMode<TResource>(this IResourceBuilder<TResource> resourceBuilder, params string[]? environments)
        where TResource : IResource =>
            (resourceBuilder as IDebugBuilder<TResource>)?.IsDebugMode ?? (
            resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode &&
            ((environments is null || environments.Length == 0) ?
                resourceBuilder.ApplicationBuilder.Environment.IsDevelopment() :
                environments.Any(resourceBuilder.ApplicationBuilder.Environment.IsEnvironment)
                ) &&
            !resourceBuilder.IsUnderTest());

    public static IResourceBuilder<TResource> WhenDebugMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> debugAction,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? notDebugAction = null,
        params string[]? environments
        )
        where TResource : IResource =>
        resourceBuilder.IsDebugMode(environments) ? debugAction(resourceBuilder) : notDebugAction?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IDebugBuilder<TResource> WithDebugger<TResource>(
        this IResourceBuilder<TResource> resourceBuilder,
        DebugMode debugMode = DebugMode.VisualStudio,
        params string[]? environments)
        where TResource : ExecutableResource
    {
        if (!resourceBuilder.IsDebugMode(environments))
        {
            return new DebugBuilder<TResource>(resourceBuilder, false);
        }
        if (debugMode == DebugMode.VisualStudio && !OperatingSystem.IsWindows())
        {
            throw new ArgumentOutOfRangeException(nameof(debugMode), "Visual Studio debugging is only supported on Windows");
        }

        resourceBuilder.ApplicationBuilder.Services.AddAttachDebuggerHook();
        return new DebugBuilder<TResource>(
            resourceBuilder
                .WithEnvironment("Launch_Debugger_On_Start",
                                 debugMode == DebugMode.Environment ? "true" : null)
                .WithAnnotation(new DebugAttachAnnotation() { DebugMode = debugMode },
                                ResourceAnnotationMutationBehavior.Replace), true);
    }

    internal static bool HasAnnotationOfType<T>(this IResource resource, Func<T, bool> predecate)
        where T : IResourceAnnotation => resource.Annotations.Any(a => a is T t && predecate(t));

    private class DebugBuilder<TResource> : IDebugBuilder<TResource>
        where TResource : IResource
    {
        public DebugBuilder(IResourceBuilder<TResource> resourceBuilder, bool isDebugMode)
        {
            ResourceBuilder = resourceBuilder;
            IsDebugMode = isDebugMode;
        }

        public IResourceBuilder<TResource> ResourceBuilder { get; }

        public IDistributedApplicationBuilder ApplicationBuilder => ResourceBuilder.ApplicationBuilder;

        public TResource Resource => ResourceBuilder.Resource;

        public bool IsDebugMode { get; }

        public IResourceBuilder<TResource> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append) where TAnnotation : IResourceAnnotation =>
            ResourceBuilder.WithAnnotation(annotation, behavior);
    }
}
