using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using C3D.Extensions.Aspire.VisualStudioDebug;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Net.NetworkInformation;
using System.Text.Json;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class IISExpressEntensions
{
    private static void ShowIISExpressHttpsEndpointInformation<T>(this T resource, ILogger logger, IISExpressBitness? bitness = null)
        where T : IResourceWithEndpoints
    {
        if (!resource.TryGetEndpoints(out var endpoints) || !endpoints.Any())
        {
            logger.LogWarning("No endpoints found for resource {ResourceName}", resource.Name);
        }

        foreach (var ep in endpoints!.Where(ep => ep.UriScheme == "https"))
        {
            bitness ??= (resource.TryGetLastAnnotation<IISExpressBitnessAnnotation>(out var bitnessAnnotations)
                ? bitnessAnnotations.Bitness : (resource as IISExpressSiteResource)?.IISExpress.Bitness)
                ?? IISExpressBitnessAnnotation.DefaultBitness;

            var port = ep.EnsureValidIISEndpointPort();
            logger.LogInformation("If your https endpoint does not work, run the following command from an elevated command prompt:\r\n" +
                "\"{Path}\\iisExpressAdminCmd.exe\" setupSslUrl -url:{Url} -UseSelfSigned",
                bitness.GetIISExpressPath().dirPath, new UriBuilder(ep.UriScheme, ep.TargetHost, port!).ToString());
        }
    }
    #region PortAllocator

    // TODO: Move this to a separate class and make it a singleton and/or accessible from DI
    // This should really be part of a 'built in' port allocator service and used by the aspire host etc.
    private static readonly int[] avoidPorts = new[] {
        5060, 5061,
        6000,
        6566,
        6665, 6666, 6667, 6668, 6669,
        6697, 10080 };

    private static BitArray? allocatedPorts;
    private static BitArray AllocatedPorts
    {
        get
        {
            if (allocatedPorts is null)
            {
                allocatedPorts = new BitArray(65536);
                foreach (var avoidPort in avoidPorts)
                {
                    allocatedPorts[avoidPort] = true;
                }

                try
                {
                    IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                    TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

                    foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
                    {
                        allocatedPorts[tcpi.LocalEndPoint.Port] = true;
                    }
                }
                catch (Exception ex)
                {
                    {
                        // Log the exception
                        System.Diagnostics.Debug.WriteLine($"Error while checking allocated ports: {ex.Message}");
                    }
                }
            }
            return allocatedPorts;
        }
    }

    public static void MarkPortAsUsed(int port)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1, nameof(port));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, nameof(port));
        var ap = AllocatedPorts;
        if (ap[port])
        {
            throw new InvalidOperationException($"Port {port} is already allocated");
        }
        ap[port] = true;
    }

    public static int GetRandomFreePort(int minPort, int maxPort)
    {
        var ap = AllocatedPorts;
        int port;
        do
        {
            port = Random.Shared.Next(minPort, maxPort);
        } while (ap[port]);
        ap[port] = true;
        return port;
    }
    #endregion

    internal static IResourceBuilder<T> ShowIISExpressHttpsEndpointInformation<T>(IResourceBuilder<T> resourceBuilder, ILogger? logger = null, IISExpressBitness? bitness = null)
        where T : IResourceWithEndpoints
    {
        var resource = resourceBuilder.Resource;
        resource.ShowIISExpressHttpsEndpointInformation(resourceBuilder.ApplicationBuilder.Eventing, logger, bitness);
        return resourceBuilder;
    }

    internal static void ShowIISExpressHttpsEndpointInformation<T>(this T resource, IDistributedApplicationEventing eventing, ILogger? logger = null, IISExpressBitness? bitness = null) where T : IResourceWithEndpoints
    {
        eventing.Subscribe<AfterEndpointsAllocatedEvent>((e, c) =>
        {
            logger ??= e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
            ShowIISExpressHttpsEndpointInformation(resource, logger, bitness);
            return Task.CompletedTask;
        });
    }
    public static IResourceBuilder<IISExpressResource> AddIISExpress(this IDistributedApplicationBuilder builder, string name, IISExpressBitness? bitness = default)
    {
        var (actualBitness, path) = bitness.GetIISExpressExe();
        var resource = new IISExpressResource(name, path, actualBitness);
        var iis = builder.AddResource(resource)
            .WithAnnotation(new AppPoolArgumentAnnotation(AppPoolArgumentAnnotation.DefaultAppPool));

        builder.Services.AddAttachDebuggerHook();

        builder.Eventing.Subscribe<BeforeStartEvent>((@event, token) =>
        {
            var notifications = @event.Services.GetRequiredService<ResourceNotificationService>();

            return notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Starting });
        });

        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((@event, token) =>
        {
            if (!iis.Resource.TryGetLastAnnotation<AppPoolArgumentAnnotation>(out var appPoolAnnotation))
            {
                throw new InvalidOperationException("IIS Express must have an AppPool defined");
            }

            var appHostConfig = iis.Resource.GetDefaultConfiguration();

            var logger = @event.Services.GetRequiredService<ResourceLoggerService>().GetLogger(iis.Resource);

            appHostConfig.SystemApplicationHost.Sites = new()
            {
                Site = [.. CreateSite(iis.Resource.Sites, appPoolAnnotation.AppPool, logger)]
            };

            var path = iis.Resource.SaveConfiguration(appHostConfig);

            logger.LogInformation("Saved configuration to '{Path}'", path);

            var notifications = @event.Services.GetRequiredService<ResourceNotificationService>();

            return notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Running });
        });

        return iis;

        static IEnumerable<Site> CreateSite(IEnumerable<IISExpressSiteResource> sites, string appPool, ILogger logger)
        {
            var id = 0;

            foreach (var site in sites)
            {
                id++;

                var tempSite = new Site()
                {
                    Name = site.Name,
                    Id = id.ToString(),
                    Application = new()
                    {
                        Path = "/",
                        ApplicationPool = appPool,
                        VirtualDirectory = new()
                        {
                            Path = "/",
                            PhysicalPath = site.WorkingDirectory
                        }
                    },
                    Bindings = new()
                    {
                        Binding = [.. CreateBindings(site)]
                    }
                };

                if (site.TryGetAnnotationsOfType<SiteConfigurationAnnotation>(out var siteConfigurators))
                {
                    foreach (var configurator in siteConfigurators)
                    {
                        configurator.Configure(tempSite);
                    }
                }

                logger.LogInformation("{Site}", JsonSerializer.Serialize(tempSite));

                yield return tempSite;
            }
        }

        static IEnumerable<Binding> CreateBindings(IResource project)
        {
            foreach (var endpoint in project.Annotations.OfType<EndpointAnnotation>())
            {
                if (endpoint.IsProxied)
                {
                    throw new InvalidOperationException("Endpoints for IIS Express must not be proxied");
                }

                yield return new Binding()
                {
                    Protocol = endpoint.UriScheme,
                    BindingInformation = $"*:{endpoint.TargetPort ?? endpoint.AllocatedEndpoint?.Port}:localhost"
                };
            }
        }
    }

    public static IResourceBuilder<IISExpressSiteResource> AddSiteProject<T>(this IResourceBuilder<IISExpressResource> builder, string name)
        where T : IProjectMetadata, new()
    {
        var project = new T();
        var resource = new IISExpressSiteResource(builder.Resource, name, Path.GetDirectoryName(project.ProjectPath)!);

        builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((@event, token) =>
        {
            if (resource.TryGetEndpoints(out var endpoints))
            {
                foreach (var endpoint in endpoints)
                {
                    endpoint.IsProxied = false;
                }
            }
            else
            {
                resource.Annotations.Add(new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: "http")
                {
                    Port = Random.Shared.Next(5000, 10000),
                    UriScheme = "http",
                    IsProxied = false
                });

                resource.Annotations.Add(new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: "https")
                {
                    Port = Random.Shared.Next(44300, 44398),
                    UriScheme = "https",
                    IsProxied = false
                });
            }

            return Task.CompletedTask;
        });

        builder.Resource.Sites.Add(resource);

        var siteResource = builder.ApplicationBuilder.AddResource(resource)
            .WithParentRelationship(builder)
            .WithAnnotation(project)
            .WithAnnotation(new SiteArgumentAnnotation(name))
            .WithAnnotation(new ConfigArgumentAnnotation(builder.Resource.GetConfigurationPath()))
            .WithArgs(c =>
            {
                foreach (var arg in resource.Annotations.OfType<IISExpressArgumentAnnotation>())
                {
                    c.Args.Add(arg);
                }
            });

        if (builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            siteResource.WithDebugger();
        }

        return siteResource;
    }

    public static IResourceBuilder<IISExpressSiteResource> WithSiteConfiguration(this IResourceBuilder<IISExpressSiteResource> builder, Action<Site> configure)
        => builder.WithAnnotation(new SiteConfigurationAnnotation(configure));

    public static IResourceBuilder<IISExpressSiteResource> WithDebugger(this IResourceBuilder<IISExpressSiteResource> resourceBuilder, DebugMode debugMode = DebugMode.VisualStudio)
        => DebugResourceBuilderExtensions.WithDebugger(resourceBuilder, debugMode)
            .WithDebugEngine(C3D.Extensions.VisualStudioDebug.WellKnown.Engines.Net4)
            .WithDebuggerHealthcheck();

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

    public static IResourceBuilder<IISExpressProjectResource> AddIISExpressProject<T>(this IDistributedApplicationBuilder builder,
        [ResourceName] string? resourceName = null,
        IISExpressBitness? bitness = null)
        where T : IProjectMetadata, new()
    {
        builder.AddIISExpressConfiguration();

        var app = new T();

        var appName = app.GetType().Name;
        var projectPath = System.IO.Path.GetDirectoryName(app.ProjectPath)!;

        (var actualBitness, var iisExpress) = bitness.GetIISExpressExe();

        resourceName ??= appName;
        var resource = new IISExpressProjectResource(resourceName, iisExpress, projectPath, actualBitness);

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

    public static IResourceBuilder<T> WithSystemWebAdapters<T>(this IResourceBuilder<T> resourceBuilder,
        string envNameBase = "RemoteApp",
        string envNameApiKey = "__ApiKey",
        string envNameUrl = "__RemoteAppUrl",
        Guid? key = null)
        where T : IResourceWithEnvironment
        => resourceBuilder
            .WithAnnotation(new SystemWebAdaptersAnnotation(key ?? Guid.NewGuid(),
                envNameBase + envNameApiKey,
                envNameBase + envNameUrl))
            .WithEnvironment(c =>
            {
                if (resourceBuilder.Resource.TryGetLastAnnotation<SystemWebAdaptersAnnotation>(out var swa))
                {
                    c.EnvironmentVariables[swa.EnvNameKey] = swa.Key.ToString();
                }
            });

    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters<T>(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        IResourceBuilder<T> iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http")
        where T : IResourceWithEndpoints
        => resourceBuilder.WithSystemWebAdapters(
            iisExpressResource.Resource,
            envNameKey,
            envNameUrl,
            endpoint);

    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters<T>(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        T iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http")
        where T : IResourceWithEndpoints
        => resourceBuilder
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
