using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Annotations;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class IISExpressArguementExtensions
{
    private static IResourceBuilder<T> WithIISExpressArguments<T>(this IResourceBuilder<T> resourceBuilder,
        params IEnumerable<IISExpressArgumentAnnotation> arguments)
        where T : IResource
    {
        foreach (var arg in arguments)
        {
            resourceBuilder.WithAnnotation(arg, ResourceAnnotationMutationBehavior.Replace);
        }
        return resourceBuilder;
    }

    public static IResourceBuilder<T> WithCustomSite<T>(this IResourceBuilder<T> resourceBuilder,
        string path,
        int port,
        string clrVersion = ClrVersionArgumentAnnotation.DefaultClrVersion)
        where T : IResource => resourceBuilder.WithIISExpressArguments(
            new PathArgumentAnnotation(path),
            new PortArgumentAnnotation(port),
            new ClrVersionArgumentAnnotation(clrVersion)
            );

    public static IResourceBuilder<T> WithCustomSite<T>(this IResourceBuilder<T> resourceBuilder,
        string path,
        int port,
        Version clrVersion)
        where T : IResource => resourceBuilder.WithIISExpressArguments(
            new PathArgumentAnnotation(path),
            new PortArgumentAnnotation(port),
            new ClrVersionArgumentAnnotation(clrVersion)
            );

    public static IResourceBuilder<T> WithIISExpressTraceLevel<T>(this IResourceBuilder<T> resourceBuilder,
        IISExpressTraceLevel traceLevel = IISExpressTraceLevel.None)
        where T : IResource => resourceBuilder.WithAnnotation(new TraceLevelArgumentAnnotation(traceLevel), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithSysTray<T>(this IResourceBuilder<T> resourceBuilder, bool showInSysTray = true)
        where T : IResource => resourceBuilder.WithAnnotation(new SysTrayArgumentAnnotation(showInSysTray), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithConfigLocation<T>(this IResourceBuilder<T> resourceBuilder, string configLocation)
        where T : IResource => resourceBuilder.WithAnnotation(new ConfigArgumentAnnotation(configLocation), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithSite<T>(this IResourceBuilder<T> resourceBuilder, string site)
        where T : IResource => resourceBuilder.WithAnnotation(new SiteArgumentAnnotation(site), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithSiteId<T>(this IResourceBuilder<T> resourceBuilder, string siteId)
        where T : IResource => resourceBuilder.WithAnnotation(new SiteIdArgumentAnnotation(siteId), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithAppPool<T>(this IResourceBuilder<T> resourceBuilder, string appPool = AppPoolArgumentAnnotation.DefaultAppPool)
        where T : IResource => resourceBuilder.WithAnnotation(new AppPoolArgumentAnnotation(appPool), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithPort<T>(this IResourceBuilder<T> resourceBuilder, int port)
        where T : IResource => resourceBuilder.WithAnnotation(new PortArgumentAnnotation(port), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithSitePath<T>(this IResourceBuilder<T> resourceBuilder, string sitePath)
        where T : IResource => resourceBuilder.WithAnnotation(new PathArgumentAnnotation(sitePath), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithClrVersion<T>(this IResourceBuilder<T> resourceBuilder, string clrVersion = ClrVersionArgumentAnnotation.DefaultClrVersion)
        where T : IResource => resourceBuilder.WithAnnotation(new ClrVersionArgumentAnnotation(clrVersion), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithClrVersion<T>(this IResourceBuilder<T> resourceBuilder, Version version)
        where T : IResource => resourceBuilder.WithAnnotation(new ClrVersionArgumentAnnotation(version), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<T> WithUserHome<T>(this IResourceBuilder<T> resourceBuilder, string userHome)
        where T : IResource => resourceBuilder.WithAnnotation(new UserHomeArgumentAnnotation(userHome), ResourceAnnotationMutationBehavior.Replace);
}
