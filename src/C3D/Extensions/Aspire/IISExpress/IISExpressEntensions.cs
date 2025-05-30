using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using C3D.Extensions.Aspire.IISExpress;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
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
            return;
        }

        var httpsEndpoints = endpoints!.Where(ep => ep.UriScheme == "https").ToList();

        foreach (var ep in httpsEndpoints)
        {
            bitness ??= (resource.TryGetLastAnnotation<IISExpressBitnessAnnotation>(out var bitnessAnnotations)
                ? bitnessAnnotations.Bitness : (resource as IISExpressSiteResource)?.IISExpress.Bitness)
                ?? IISExpressBitnessAnnotation.DefaultBitness;

            var port = ep.EnsureValidIISEndpointPort();
            logger.LogInformation("If your https endpoint does not work, run the following command from an elevated command prompt:\r\n" +
                "\"{Path}\\iisExpressAdminCmd.exe\" setupSslUrl -url:{Url} -UseSelfSigned",
                bitness.GetIISExpressPath().dirPath, new UriBuilder(ep.UriScheme, ep.TargetHost, port!).ToString());
        }

        if (httpsEndpoints.Count != 0)
        {
            resource.Annotations.Add(new ResourceCommandAnnotation("fix-https",
                "Fix https", update =>
                {
                    return ResourceCommandState.Enabled;
                },
                async execute =>
                {
                    return await ExecuteFixHttpsCommand(execute, bitness, httpsEndpoints);
                },
                "If your https endpoints do not work, this should resolve things (Runs as Admin)",
                httpsEndpoints,
                "This requires admin privileges to run. You may get a UAC prompt.",
                "Wrench",
                IconVariant.Regular,
                false));
        }
    }

    public static async Task ExecuteFixHttpsCommand<T>(this T resource, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where T : IResourceWithEndpoints
    {
        if (!resource.TryGetEndpoints(out var endpoints) || !endpoints.Any())
        {
            throw new InvalidOperationException("No endpoints found for resource");
        }
        var httpsEndpoints = endpoints!.Where(ep => ep.UriScheme == "https").ToList();
        if (httpsEndpoints.Count == 0)
        {
            throw new InvalidOperationException("No https endpoints found for resource");
        }

        var bitness = (resource.TryGetLastAnnotation<IISExpressBitnessAnnotation>(out var bitnessAnnotations)
            ? bitnessAnnotations.Bitness : (resource as IISExpressSiteResource)?.IISExpress.Bitness)
            ?? IISExpressBitnessAnnotation.DefaultBitness;
        var context = new ExecuteCommandContext()
        {
            CancellationToken = cancellationToken,
            ResourceName = resource.Name,
            ServiceProvider = serviceProvider,
        };
        await ExecuteFixHttpsCommand(context, bitness, httpsEndpoints);
    }

    private static async Task<ExecuteCommandResult> ExecuteFixHttpsCommand(ExecuteCommandContext execute, IISExpressBitness? bitness, List<EndpointAnnotation> httpsEndpoints)
    {
        var cmd = execute.ServiceProvider.GetRequiredService<CommandExecutor>();
        var logger = execute.ServiceProvider.GetRequiredService<ResourceLoggerService>().GetLogger(execute.ResourceName);

        var command = System.IO.Path.Combine(bitness.GetIISExpressPath().dirPath, "iisExpressAdminCmd.exe");
        if (httpsEndpoints.Count == 1)
        {
            var ep = httpsEndpoints[0];
            var port = ep.EnsureValidIISEndpointPort();
            var exitCode = await cmd.ExecuteAdminCommandAsync(command, logger, "setupSslUrl",
                $"-url:{ep.UriScheme}://localhost:{port}",
                "-UseSelfSigned"
                );
            if (exitCode != 0)
            {
                return new ExecuteCommandResult() { Success = false, ErrorMessage = $"ExitCode: {exitCode}" };
            }
        }
        else
        {

            var batName = Path.ChangeExtension(Path.GetTempFileName(), ".bat");
            try
            {
                using (var bat = File.CreateText(batName))
                {
                    bat.WriteLine("@echo off");
                    foreach (var ep in httpsEndpoints)
                    {
                        var port = ep.EnsureValidIISEndpointPort();
                        var queuedCommand = $"\"{command}\" setupSslUrl -url:{ep.UriScheme}://localhost:{port} -UseSelfSigned";
                        logger.LogInformation("Queueing command: {Command}", queuedCommand);
                        await bat.WriteLineAsync(queuedCommand);
                    }
                }
                var exitCode = await cmd.ExecuteAdminCommandAsync(batName, logger);
                if (exitCode != 0)
                {
                    return new ExecuteCommandResult() { Success = false, ErrorMessage = $"ExitCode: {exitCode}" };
                }
            }
            catch (Exception ex)
            {
                return new ExecuteCommandResult() { Success = false, ErrorMessage = ex.Message };
            }
            finally
            {
                try { File.Delete(batName); } catch { }
            }
        }
        return new ExecuteCommandResult() { Success = true };
    }

    internal static T EnsureValidIISEndpoints<T>(this T resource)
        where T : IResourceWithEndpoints
    {
        foreach (var ep in resource.Annotations.OfType<EndpointAnnotation>())
        {
            ep.EnsureValidIISEndpointPort();
        }
        return resource;
    }

    #region PortAllocator

    // TODO: Move this to a separate class and make it a singleton and/or accessible from DI
    // This should really be part of a 'built in' port allocator service and used by the aspire host etc.
    private static readonly int[] avoidPorts = [
        5060, 5061,
        6000,
        6566,
        6665, 6666, 6667, 6668, 6669,
        6697, 10080 ];

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

    public static bool TryMarkPortAsUsed(int port)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1, nameof(port));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, nameof(port));
        var ap = AllocatedPorts;
        if (ap[port])
        {
            return false; // Port is already allocated
        }
        ap[port] = true;
        return true; // Successfully marked the port as used
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

    private static int EnsureValidIISEndpointPort(this EndpointAnnotation endpoint)
    {
        var port = endpoint.TargetPort ?? endpoint.AllocatedEndpoint?.Port;
        if (port is null)
        {
            port = endpoint.UriScheme switch
            {
                "http" => GetRandomFreePort(5000, 10000),  // There are certain ports that browsers will object to that should be avoided.
                                                           // See https://github.com/CZEMacLeod/C3D.Extensions.Playwright.AspNetCore/blob/6d3b92790df905cd44587c983c2b89546a856ee3/src/C3D/Extensions/Playwright/AspNetCore/Factory/PlaywrightWebApplicationFactory.cs#L29
                                                           // Search for --explicitly-allowed-ports or network.security.ports.banned.override
                "https" => GetRandomFreePort(44300, 44400),
                _ => throw new InvalidOperationException($"Unsupported uri scheme: {endpoint.UriScheme}")
            };
            endpoint.TargetPort = port;
        }

        return port!.Value;
    }

    internal static Site CreateIISConfigSite(this IISExpressSiteResource site, int id, string appPool, ILogger logger)
    {
        var tempSite = CreateIISConfigSite(site, id, site.Name, site.WorkingDirectory, appPool, logger);
        ShowIISExpressHttpsEndpointInformation(site, logger);
        return tempSite;
    }

    internal static Site CreateIISConfigSite(this IResourceWithEndpoints site, int id, string name, string path, string appPool, ILogger logger)
    {
        var tempSite = new Site()
        {
            Name = name,
            Id = id.ToString(),
            Application = new()
            {
                Path = "/",
                ApplicationPool = appPool,
                VirtualDirectory = new()
                {
                    Path = "/",
                    PhysicalPath = path
                }
            },
            Bindings = new()
            {
                Binding = [.. site.CreateIISConfigBindings()]
            }
        };

        if (site.TryGetAnnotationsOfType<SiteConfigurationAnnotation>(out var siteConfigurators))
        {
            foreach (var configurator in siteConfigurators)
            {
                configurator.Configure(tempSite);
            }
        }

        logger.LogDebug("{Site}", JsonSerializer.Serialize(tempSite));
        return tempSite;
    }

    internal static IEnumerable<Binding> CreateIISConfigBindings(this IResourceWithEndpoints project)
    {
        foreach (var endpoint in project.Annotations.OfType<EndpointAnnotation>())
        {
            yield return CreateIISConfigBinding(endpoint);
        }
    }

    internal static Binding CreateIISConfigBinding(this EndpointAnnotation endpoint)
    {
        //if (endpoint.IsProxied)
        //{
        //    throw new InvalidOperationException("Endpoints for IIS Express must not be proxied");
        //}

        int? port = EnsureValidIISEndpointPort(endpoint);

        return new Binding()
        {
            Protocol = endpoint.UriScheme,
            BindingInformation = $"*:{port}:{endpoint.TargetHost}"
        };
    }

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

    internal static EndpointAnnotation AddOrUpdateEndpointFromBinding(this IResourceWithEndpoints resource, Binding binding, ILogger? logger = null)
    {
        var hostName = string.IsNullOrEmpty(binding.HostName) ? "localhost" : binding.HostName;

        var endpoint = resource.Annotations
                            .OfType<EndpointAnnotation>()
                            .Where(ea => StringComparer.OrdinalIgnoreCase.Equals(ea.Name, binding.Protocol) &&
                                         StringComparer.OrdinalIgnoreCase.Equals(ea.TargetHost, hostName))
                            .SingleOrDefault();
        if (endpoint is null)
        {
            endpoint = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: binding.Protocol);
            resource.Annotations.Add(endpoint);
        }
        else if (endpoint.TargetPort is not null && endpoint.TargetPort != binding.Port)
        {
            endpoint = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: $"{binding.Protocol}-{binding.Port}-{hostName}");
            resource.Annotations.Add(endpoint);
        }
        if (StringComparer.OrdinalIgnoreCase.Equals(hostName, "localhost"))
        {
            MarkPortAsUsed(binding.Port);
        }
        else
        {
            if (!TryMarkPortAsUsed(binding.Port))
            {
                logger?.LogWarning("Port {Port} is already allocated. Ensure each binding has a unique hostName.", binding.Port);
            }
            logger?.LogInformation("It is recommended to use 'localhost' with unique ports for IIS Express endpoints to avoid port conflicts.");
            // If the hostName is not localhost, we disable the proxy support so that SNI and host name routing works as expected.
            endpoint.IsProxied = false;
        }

        endpoint.TargetPort = binding.Port;
        endpoint.TargetHost = hostName;
        endpoint.UriScheme = binding.Protocol;

        return endpoint;
    }

    public static IResourceBuilder<IISExpressResource> AddIISExpress(this IDistributedApplicationBuilder builder, string name, IISExpressBitness? bitness = default)
    {
        var (actualBitness, path) = bitness.GetIISExpressExe();
        var resource = new IISExpressResource(name, path, actualBitness);
        var iis = builder.AddResource(resource)
            .WithAnnotation(new AppPoolArgumentAnnotation(AppPoolArgumentAnnotation.DefaultAppPool));

        builder.Services.AddSingleton<CommandExecutor>();

        if (builder.Environment.IsDevelopment() && builder.ExecutionContext.IsRunMode)
        {
            builder.Services.AddAttachDebuggerHook();
        }

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
                Site = [.. CreateSites(iis.Resource.Sites, appPoolAnnotation.AppPool, logger)]
            };

            var path = iis.Resource.SaveConfiguration(appHostConfig);

            logger.LogInformation("Saved configuration to '{Path}'", path);

            var notifications = @event.Services.GetRequiredService<ResourceNotificationService>();

            return notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Running });
        });

        return iis;

        static IEnumerable<Site> CreateSites(IEnumerable<IISExpressSiteResource> sites, string appPool, ILogger logger)
        {
            var id = 0;

            foreach (var site in sites)
            {
                id++;
                yield return site.CreateIISConfigSite(id, appPool, logger);
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
            if (!resource.TryGetEndpoints(out var endpoints))
            {
                resource.WithDefaultIISExpressEndpoints();
            }
            resource.EnsureValidIISEndpoints();

            // Inherit the default apppool if not explicitly set
            if (!resource.HasAnnotationOfType<AppPoolArgumentAnnotation>() && resource.IISExpress.TryGetLastAnnotation<AppPoolArgumentAnnotation>(out var appPool))
            {
                resource.Annotations.Add(new AppPoolArgumentAnnotation(appPool.AppPool));
            }

            return Task.CompletedTask;
        });

        builder.Resource.Sites.Add(resource);

        return builder.ApplicationBuilder.AddResource(resource)
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
            })
            .WithDebugger();
    }

    /// <summary>
    /// Sets up the default endpoints for IIS Express. This will add http and https endpoints to the resource.
    /// </summary>
    /// <remarks>
    /// When a valid IIS Express config file is found and used, the ports from the file will be used if they are left null.
    /// If the config file does not exist, or the ports are not set in the config file, then a port in the range 5000-10000 will be used for http and 44300-44399 for https.
    /// If you specify a port, it will be used in addition to any ground in config file.
    /// If you don't want to use the config file, you can use the <see cref="WithTemporaryConfig(IResourceBuilder{IISExpressProjectResource})"/> method to create a temporary config file.
    /// </remarks>
    /// <param name="httpPort">Explicit http port. If null then the config file port, or a port in the range 5000-10000 will be used</param>
    /// <param name="httpsPort">Explicit https port. If null then the config file port, or a port in the range 44300-44399 will be used</param>
    /// <param name="isProxied">Set to true if the endpoint should be proxied or false if the endpoint should not be proxied. Defaults to no change which will normally be true.</param>
    public static IResourceBuilder<T> WithDefaultIISExpressEndpoints<T>(this IResourceBuilder<T> resourceBuilder,
        int? httpPort = null, int? httpsPort = null, bool? isProxied = null)
        where T : IResourceWithEndpoints
    {
        resourceBuilder.WithEndpoint("http", ep =>
        {
            ep.UriScheme = "http";
            if (isProxied.HasValue) ep.IsProxied = isProxied.Value;
            ep.TargetPort ??= httpPort;
        });
        resourceBuilder.WithEndpoint("https", ep =>
        {
            ep.UriScheme = "https";
            if (isProxied.HasValue) ep.IsProxied = isProxied.Value;
            ep.TargetPort ??= httpsPort;
        });

        return resourceBuilder;
    }

    public static T WithDefaultIISExpressEndpoints<T>(this T resource, int? httpPort = null, int? httpsPort = null, bool isProxied = true)
        where T : IResourceWithEndpoints
    {
        bool addHttp = true;
        bool addHttps = true;
        if (resource.TryGetEndpoints(out var endpoints))
        {
            addHttp = !endpoints.Any(e => e.Name == "http");
            addHttps = !endpoints.Any(e => e.Name == "https");
        }
        if (addHttp)
        {
            resource.Annotations.Add(new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: "http")
            {
                IsProxied = isProxied,
                TargetPort = httpPort,
                UriScheme = "http"
            });
        }
        if (addHttps)
        {
            resource.Annotations.Add(new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: "https")
            {
                IsProxied = isProxied,
                TargetPort = httpsPort,
                UriScheme = "https"
            });
        }
        return resource;
    }

    public static IResourceBuilder<IISExpressSiteResource> WithSiteConfiguration(this IResourceBuilder<IISExpressSiteResource> builder, Action<Site> configure)
        => builder.WithAnnotation(new SiteConfigurationAnnotation(configure));

    public static IDistributedApplicationBuilder AddIISExpressConfiguration(this IDistributedApplicationBuilder builder,
        Action<IISExpressOptions>? options = null)
    {
        options ??= _ => { };
        var o = builder.Services
            .AddOptions<IISExpressOptions>()
            .Configure(o =>
            {
                o.AssemblyName = builder.AppHostAssembly?.FullName;
                o.Assembly = builder.AppHostAssembly;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        if (options is not null)
            o.Configure(options);

        builder.Services.AddTransient<IISEndPointConfigurator>();

        builder.Services.AddSingleton<CommandExecutor>();

        builder.Eventing.Subscribe<BeforeStartEvent>((@event, cancellationToken) =>
        {
            var services = @event.Services;
            var configurator = services.GetRequiredService<IISEndPointConfigurator>();

            configurator.Configure();

            return Task.CompletedTask;
        });

        if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
        {
            builder.Services.AddAttachDebuggerHook();
        }

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

        return builder.AddResource(resource)
            .WithAnnotation(app)
            .WithAnnotation(new SiteArgumentAnnotation(appName))
            .WithArgs(c =>
                {
                    foreach (var arg in resource.Annotations.OfType<IISExpressArgumentAnnotation>())
                    {
                        c.Args.Add(arg);
                    }
                })
            .WithOtlpExporter()
            .ExcludeFromManifest()
            .WithDebugger();
    }

    /// <summary>
    /// Creates a temporary config file for IIS Express. This will be created in the temp directory.
    /// </summary>
    public static IResourceBuilder<IISExpressProjectResource> WithTemporaryConfig(this IResourceBuilder<IISExpressProjectResource> resourceBuilder) =>
        resourceBuilder.WithConfigLocation(ApplicationHostConfigurationExtensions.GetTempConfigFile());

    /// <summary>
    /// Sets the config location for the IIS Express project.
    /// </summary>
    /// <remarks>
    /// The file will be created if it does not already exist.
    /// If the path is not fully qualified, it will be combined with the application host directory.
    /// </remarks>
    public static IResourceBuilder<IISExpressProjectResource> WithConfigLocation(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string configLocation)
    {
        if (!System.IO.Path.IsPathFullyQualified(configLocation))
        {
            configLocation = System.IO.Path.Combine(resourceBuilder.ApplicationBuilder.AppHostDirectory, configLocation);
        }
        return resourceBuilder.WithAnnotation(new ConfigArgumentAnnotation(configLocation), ResourceAnnotationMutationBehavior.Replace);
    }

    public static IResourceBuilder<IISExpressProjectResource> WithSiteName(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string siteName) => resourceBuilder.WithAnnotation(new SiteArgumentAnnotation(siteName), ResourceAnnotationMutationBehavior.Replace);

    public static IResourceBuilder<IISExpressProjectResource> WithAppPool(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
        string appPoolName) => resourceBuilder.WithAnnotation(new AppPoolArgumentAnnotation(appPoolName), ResourceAnnotationMutationBehavior.Replace);

    internal static Add CreateIISConfigApplicationPool(this AppPoolArgumentAnnotation appPool) => new()
    {
        Name = appPool.AppPool,
        ManagedRuntimeVersion = "v4.0",
        ManagedPipelineMode = "Integrated",
        CLRConfigFile = "%IIS_USER_HOME%\\config\\aspnet.config",
        AutoStart = "true"
    };
}
