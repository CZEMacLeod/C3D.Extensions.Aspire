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
                project.Annotations.Add(new ConfigArgumentAnnotation(appHostConfig));
            }

            if (project.TryGetLastAnnotation<SiteArgumentAnnotation>(out var site) &&
                project.TryGetLastAnnotation<ConfigArgumentAnnotation>(out var cfg))
            {
                AddBindings(project, site, cfg);
            }
        }
    }

    private static void AddBindings(IISExpressProjectResource project, SiteArgumentAnnotation site, ConfigArgumentAnnotation cfg)
    {
        var siteConfig = GetSiteConfig(cfg.ApplicationHostConfig, site.Site);

        if (!project.HasAnnotationOfType<AppPoolArgumentAnnotation>())
        {
            project.Annotations.Add(new AppPoolArgumentAnnotation(siteConfig?.Application.ApplicationPool ?? AppPoolArgumentAnnotation.DefaultAppPool));
        }

        if (siteConfig is not null)
        {
            foreach (var binding in siteConfig.Bindings)
            {
                AddBinding(project, binding);
            }
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

        return appHostConfig.SystemApplicationHost.Sites
            .SingleOrDefault(s => string.Equals(s.Name, siteName, StringComparison.OrdinalIgnoreCase));
    }
}
