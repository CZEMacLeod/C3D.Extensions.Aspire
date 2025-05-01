using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DebugResourceBuilderExtensions
{
    public static bool IsDebugMode<TResource>(this IResourceBuilder<TResource> resourceBuilder)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.ExecutionContext.IsRunMode &&
            resourceBuilder.ApplicationBuilder.Environment.IsDevelopment() &&
            !resourceBuilder.IsUnderTest();

    public static bool IsUnderTest<TResource>(this IResourceBuilder<TResource> _)
        where TResource : IResource => new StackTrace().HasTestInStackTrace();

    public static bool IsUnderTest(this IDistributedApplicationBuilder _) => new StackTrace().HasTestInStackTrace();
    public static bool IsUnderTest(this IHostEnvironment _) => new StackTrace().HasTestInStackTrace();

    public static IResourceBuilder<TResource> WhenDebugMode<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Action<IResourceBuilder<TResource>> debugAction,
        Action<IResourceBuilder<TResource>>? notDebugAction = null
        )
        where TResource : IResource
    {
        if ((resourceBuilder as IDebugBuilder<TResource>)?.IsDebugMode ?? resourceBuilder.IsDebugMode())
        {
            debugAction(resourceBuilder);
        }
        else
        {
            notDebugAction?.Invoke(resourceBuilder);
        }

        return resourceBuilder;
    }

    public static IResourceBuilder<TResource> WhenUnderTest<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Action<IResourceBuilder<TResource>> testAction,
        Action<IResourceBuilder<TResource>>? notTestAction = null
    )
        where TResource : IResource
    {
        if (resourceBuilder.IsUnderTest())
        {
            testAction(resourceBuilder);
        }
        else
        {
            notTestAction?.Invoke(resourceBuilder);
        }

        return resourceBuilder;
    }

    internal static bool HasTestInStackTrace(this StackTrace callStack) =>
        callStack.GetStackFrames().Any(sf =>
            sf.GetMethod()?.DeclaringType?.Assembly.GetName().Name == "Aspire.Hosting.Testing");

    private static StackFrame[] GetStackFrames(this StackTrace callStack) => callStack.GetFrames() ?? [];

    public static IDebugBuilder<TResource> WithDebugger<TResource>(
        this IResourceBuilder<TResource> resourceBuilder,
        DebugMode debugMode = DebugMode.VisualStudio)
        where TResource : ExecutableResource
    {
        if (!resourceBuilder.IsDebugMode())
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
                .WithAnnotation<DebugAttachAnnotation>(new()
                {
                    DebugMode = debugMode
                },
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
