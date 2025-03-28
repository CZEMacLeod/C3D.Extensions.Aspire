using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Resources;
using C3D.Extensions.Aspire.VisualStudioDebug;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class IISExpressEntensions
{
    public static IResourceBuilder<IISExpressProjectResource> WithDebugger(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        DebugMode debugMode = DebugMode.VisualStudio) =>
        DebugResourceBuilderExtensions.WithDebugger(resourceBuilder, debugMode)
            .WithDebugEngine(C3D.Extensions.VisualStudioDebug.WellKnown.Engines.Net4)
            .WithDebuggerHealthcheck();

    public static IDistributedApplicationBuilder AddIISExpressConfiguration(this IDistributedApplicationBuilder builder,
        Action<IISExpressOptions>? options = null)
    {
        options ??= _ => { };
        var o = builder.Services
            .AddOptions<IISExpressOptions>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
        if (options is not null)
            o.Configure(options);

        builder.Services.AddTransient<IISEndPointConfigurator>();
        
        builder.Eventing.Subscribe<BeforeStartEvent>((@event, cancellationToken) =>
        {
            var services = @event.Services;
            var configurator = services.GetRequiredService<IISEndPointConfigurator>();

            configurator.Configure();

            return Task.CompletedTask;
        });

        builder.Services.AddAttachDebuggerHook();

        return builder;
    }

    private static readonly Dictionary<IISExpressBitness, string> iisExpressPath = new();

    public static IResourceBuilder<IISExpressProjectResource> AddIISExpressProject<T>(this IDistributedApplicationBuilder builder,
        [ResourceName] string? resourceName = null,
        IISExpressBitness? bitness = null)
        where T : IProjectMetadata, new()
    {
        builder.AddIISExpressConfiguration();

        var app = new T();

        var appName = app.GetType().Name;
        var projectPath = System.IO.Path.GetDirectoryName(app.ProjectPath)!;

        bitness ??= Environment.Is64BitOperatingSystem ? IISExpressBitness.IISExpress64Bit : IISExpressBitness.IISExpress32Bit;

        if (!iisExpressPath.TryGetValue(bitness.Value, out var iisExpress))
        {
            var programFiles = System.Environment.GetFolderPath(bitness == IISExpressBitness.IISExpress32Bit ?
                Environment.SpecialFolder.ProgramFilesX86 :
                Environment.SpecialFolder.ProgramFiles);
            iisExpress = System.IO.Path.Combine(programFiles, "IIS Express", "iisexpress.exe");
            iisExpressPath[bitness.Value] = iisExpress;
        }

        resourceName ??= appName;
        var resource = new IISExpressProjectResource(resourceName, iisExpress, projectPath);

        var resourceBuilder = builder.AddResource(resource)
            .WithAnnotation(app)
            //.WithAnnotation(new AppPoolArgumentAnnotation())
            .WithAnnotation(new SiteArgumentAnnotation(appName))
            .WithArgs(c =>
                {
                    foreach (var arg in resource.Annotations.OfType<IISExpressArgumentAnnotation>())
                    {
                        c.Args.Add(arg);
                    }
                })
            .WithOtlpExporter()
            .ExcludeFromManifest();

        if (builder.Environment.IsDevelopment())
        {
            resourceBuilder.WithDebugger();
        }

        return resourceBuilder;
    }

    public static IResourceBuilder<IISExpressProjectResource> WithConfigLocation(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string configLocation) => resourceBuilder.WithAnnotation(new ConfigArgumentAnnotation(configLocation), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<IISExpressProjectResource> WithSiteName(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string siteName) => resourceBuilder.WithAnnotation(new SiteArgumentAnnotation(siteName), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<IISExpressProjectResource> WithAppPool(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string appPoolName) => resourceBuilder.WithAnnotation(new AppPoolArgumentAnnotation(appPoolName), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<IISExpressProjectResource> WithSystemWebAdapters(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string envNameBase = "RemoteApp",
        string envNameApiKey = "__ApiKey",
        string envNameUrl = "__RemoteAppUrl",
        Guid? key = null) =>
        resourceBuilder
            .WithAnnotation(new SystemWebAdaptersAnnotation(key ?? Guid.NewGuid(),
                envNameBase + envNameApiKey,
                envNameBase + envNameUrl))
            .WithEnvironment(c=>
            {
                if (resourceBuilder.Resource.TryGetLastAnnotation<SystemWebAdaptersAnnotation>(out var swa))
                {
                    c.EnvironmentVariables[swa.EnvNameKey] = swa.Key.ToString();
                }
            });

    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        IResourceBuilder<IISExpressProjectResource> iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http") => resourceBuilder.WithSystemWebAdapters(
            iisExpressResource.Resource,
            envNameKey,
            envNameUrl,
            endpoint);

    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        IISExpressProjectResource iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http") =>
        resourceBuilder
            .WithRelationship(iisExpressResource, "YARP")
            .WithEnvironment(c =>
            {
                if (iisExpressResource.TryGetLastAnnotation<SystemWebAdaptersAnnotation>(out var swa))
                {
                    c.EnvironmentVariables[envNameKey ?? swa.EnvNameKey] = swa.Key.ToString();
                    c.EnvironmentVariables[envNameUrl ?? swa.EnvNameUrl] = iisExpressResource.GetEndpoint(endpoint);
                }
            });
}
