using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class EnvironmentExtensions
{
    public static IResourceBuilder<TResource> WhenHostEnvironment<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        string environmentName,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> matchedEnviroment,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherEnvironments)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.Environment.IsEnvironment(environmentName) ?
            matchedEnviroment(resourceBuilder) :
            otherEnvironments?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenHostEnvironment<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> matchedEnviroment,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherEnvironments,
        params string[] environments)
        where TResource : IResource => environments.Any(resourceBuilder.ApplicationBuilder.Environment.IsEnvironment) ?
            matchedEnviroment(resourceBuilder) :
            otherEnvironments?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenDevelopment<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> developmentEnvironment,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherEnvironments)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.Environment.IsDevelopment() ?
            developmentEnvironment(resourceBuilder) :
            otherEnvironments?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenStaging<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> stagingEnvironment,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherEnvironments)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.Environment.IsStaging() ?
            stagingEnvironment(resourceBuilder) :
            otherEnvironments?.Invoke(resourceBuilder) ?? resourceBuilder;

    public static IResourceBuilder<TResource> WhenProduction<TResource>(this IResourceBuilder<TResource> resourceBuilder,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>> productionEnvironment,
        Func<IResourceBuilder<TResource>, IResourceBuilder<TResource>>? otherEnvironments)
        where TResource : IResource => resourceBuilder.ApplicationBuilder.Environment.IsProduction() ?
            productionEnvironment(resourceBuilder) :
            otherEnvironments?.Invoke(resourceBuilder) ?? resourceBuilder;
}
