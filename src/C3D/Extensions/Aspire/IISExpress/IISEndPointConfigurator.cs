using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

internal class IISEndPointConfigurator
{
    private readonly DistributedApplicationModel appModel;
    private readonly IOptions<IISExpressOptions> options;
    private readonly ILogger<IISEndPointConfigurator> logger;

    public IISEndPointConfigurator(DistributedApplicationModel appModel, IOptions<IISExpressOptions> options, ILogger<IISEndPointConfigurator> logger)
    {
        this.appModel = appModel;
        this.options = options;
        this.logger = logger;
    }

    public void Configure()
    {
        var appHostConfig = options.Value.ApplicationHostConfig!;
        foreach (var project in appModel.Resources.OfType<IISExpressProjectResource>())
        {
            if (!project.HasAnnotationOfType<ConfigArgumentAnnotation>())
            {
                if (project.TryGetLastAnnotation<SiteArgumentAnnotation>(out var siteArg))
                {
                    var tempConfigPath = AddApplicationHostConfiguration(project, siteArg);

                    project.Annotations.Add(new ConfigArgumentAnnotation(tempConfigPath));
                }
                else
                {
                    project.Annotations.Add(new ConfigArgumentAnnotation(appHostConfig));
                }
            }

            if (project.TryGetLastAnnotation<SiteArgumentAnnotation>(out var site) &&
                project.TryGetLastAnnotation<ConfigArgumentAnnotation>(out var cfg))
            {
                AddBindings(project, site, cfg);
            }
        }
    }

    private static string AddApplicationHostConfiguration(IISExpressProjectResource project, SiteArgumentAnnotation siteArg)
    {
        AppPoolArgumentAnnotation? appPoolAnnotation;
        if (!project.TryGetLastAnnotation(out appPoolAnnotation))
        {
            appPoolAnnotation = new AppPoolArgumentAnnotation(AppPoolArgumentAnnotation.DefaultAppPool);
            project.Annotations.Add(appPoolAnnotation);
        }

        var tempSite = new Site()
        {
            Name = siteArg.Site,
            Id = "1",
            Application = new()
            {
                Path = "/",
                ApplicationPool = appPoolAnnotation.AppPool,
                VirtualDirectory = new()
                {
                    Path = "/",
                    PhysicalPath = project.WorkingDirectory
                }
            },
            Bindings = new()
            {
                Binding = [.. CreateBindings(project)]
            }
        };

        return CreateTemporaryHostConfig(project, tempSite);

        static IEnumerable<Binding> CreateBindings(IISExpressProjectResource project)
        {
            var hasEndpoints = false;

            if (project.TryGetEndpoints(out var endpoints))
            {
                foreach (var endpoint in endpoints)
                {
                    if (endpoint.IsProxied)
                    {
                        throw new InvalidOperationException("Endpoints for IIS Express must not be proxied");
                    }

                    hasEndpoints = true;

                    yield return new Binding()
                    {
                        // Use the endpoints from the project
                        Protocol = endpoint.UriScheme,
                        BindingInformation = $"*:{endpoint.TargetPort ?? endpoint.AllocatedEndpoint?.Port}:localhost"
                    };
                }
            }

            if (!hasEndpoints)
            {
                yield return new Binding
                {
                    Protocol = "http",
                    BindingInformation = $"*:{Random.Shared.Next(5000, 10000)}:localhost"
                };

                yield return new Binding
                {
                    Protocol = "https",
                    BindingInformation = $"*:{Random.Shared.Next(44300, 44398)}:localhost"
                };
            }
        }
    }

    private void AddBindings(IISExpressProjectResource project, SiteArgumentAnnotation site, ConfigArgumentAnnotation cfg)
    {
        var siteConfig = GetSiteConfig(cfg.ApplicationHostConfig, site.Site);

        if (!project.HasAnnotationOfType<AppPoolArgumentAnnotation>())
        {
            project.Annotations.Add(new AppPoolArgumentAnnotation(siteConfig?.Application.ApplicationPool ?? AppPoolArgumentAnnotation.DefaultAppPool));
        }

        if (siteConfig is not null)
        {
            if (project.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
            {
                var sitePath = siteConfig.Application.VirtualDirectory.PhysicalPath;
                var projectPath = System.IO.Path.GetDirectoryName(metadata.ProjectPath);
                if (!sitePath.Equals(projectPath))
                {
                    logger.LogWarning("Site {Site} physical path {SitePath} does not match project path {ProjectPath}", site.Site, sitePath, projectPath);
                }
            }
            foreach (var binding in siteConfig.Bindings.Binding)
            {
                AddBinding(project, binding);
            }
        }
        else
        {
            logger.LogError("Site {Site} not found in {AppHostConfig}", site.Site, cfg.ApplicationHostConfig);
        }
    }

    private static void AddBinding(IISExpressProjectResource project, Binding binding)
    {
        var endpoint = project.Annotations
                            .OfType<EndpointAnnotation>()
                            .Where(ea => StringComparer.OrdinalIgnoreCase.Equals(ea.Name, binding.Protocol))
                            .SingleOrDefault();

        if (endpoint is null)
        {
            endpoint = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: binding.Protocol);
            project.Annotations.Add(endpoint);
        }

        endpoint.Port = binding.Port;
        endpoint.UriScheme = binding.Protocol;
        endpoint.IsProxied = false;
    }

    private static Site? GetSiteConfig(string appHostConfigPath, string siteName)
    {
        var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));
        using var reader = new FileStream(appHostConfigPath, FileMode.Open);

        if (serializer.Deserialize(reader) is not ApplicationHostConfiguration appHostConfig)
        {
            return null;
        }

        return appHostConfig.SystemApplicationHost.Sites.Site
            .SingleOrDefault(s => string.Equals(s.Name, siteName, StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateTemporaryHostConfig(IISExpressProjectResource iis, Site site)
    {
        var iisDirectory = Path.GetDirectoryName(iis.Command);

        if (iisDirectory is null)
        {
            throw new InvalidOperationException("Could not find IIS Express directory");
        }

        var defaultHostConfig = Path.Combine(iisDirectory, "config", "templates", "PersonalWebServer", "applicationHost.config");

        if (!File.Exists(defaultHostConfig))
        {
            throw new FileNotFoundException("Could not find default host config", defaultHostConfig);
        }

        using var defaultHostConfigStream = File.OpenRead(defaultHostConfig);
        var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));

        var config = serializer.Deserialize(defaultHostConfigStream) as ApplicationHostConfiguration ?? throw new InvalidOperationException("Could not parse default host config");

        config.SystemApplicationHost.Sites.Site = [site];

        var path = Path.GetTempFileName();
        using var writer = File.OpenWrite(path);

        serializer.Serialize(writer, config);

        return path;
    }
}
