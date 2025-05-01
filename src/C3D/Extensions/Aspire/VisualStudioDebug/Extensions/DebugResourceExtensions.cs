using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using Microsoft.Extensions.Hosting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DebugResourceBuilderExtensions
{
    public static IDebugBuilder<TResource> WithDebugger<TResource>(
        this IResourceBuilder<TResource> resourceBuilder,
        DebugMode debugMode = DebugMode.VisualStudio)
        where TResource : ExecutableResource
    {
        if (!resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode && resourceBuilder.ApplicationBuilder.Environment.IsDevelopment())
        {
            return new DebugBuilder<TResource>(resourceBuilder);
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
                .WithAnnotation<DebugAttachAnnotation>(new()
                {
                    DebugMode = debugMode
                },
                    ResourceAnnotationMutationBehavior.Replace));
    }

    internal static bool HasAnnotationOfType<T>(this IResource resource, Func<T, bool> predecate)
        where T : IResourceAnnotation => resource.Annotations.Any(a => a is T t && predecate(t));

    private class DebugBuilder<TResource> : IDebugBuilder<TResource>
        where TResource : IResource
    {
        private readonly IResourceBuilder<TResource> resourceBuilder;

        public DebugBuilder(IResourceBuilder<TResource> resourceBuilder) =>
            this.resourceBuilder = resourceBuilder;

        public IResourceBuilder<TResource> ResourceBuilder => resourceBuilder;

        public IDistributedApplicationBuilder ApplicationBuilder => resourceBuilder.ApplicationBuilder;

        public TResource Resource => resourceBuilder.Resource;

        public IResourceBuilder<TResource> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append) where TAnnotation : IResourceAnnotation =>
            resourceBuilder.WithAnnotation(annotation, behavior);
    }
}
