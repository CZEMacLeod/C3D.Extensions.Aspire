using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

internal class IISEndPointConfigurator
{
    private readonly IOptions<IISExpressOptions> options;
    private readonly ILogger<IISEndPointConfigurator> logger;
    private readonly CommandExecutor commandExecutor;
    private readonly IServiceProvider serviceProvider;
    private readonly IISExpressProjectResource project;

    public IISEndPointConfigurator(
        IOptions<IISExpressOptions> options,
        ILogger<IISEndPointConfigurator> logger,
        CommandExecutor commandExecutor,
        IServiceProvider serviceProvider,
        IISExpressProjectResource project)
    {
        this.options = options;
        this.logger = logger;
        this.commandExecutor = commandExecutor;
        this.serviceProvider = serviceProvider;
        this.project = project;
    }

    public Task ConfigureBeforeStartAsync()
    {
        var appHostConfig = options.Value.ApplicationHostConfig!;
        if (!project.HasAnnotationOfType<ConfigArgumentAnnotation>())
        {
            project.Annotations.Add(new ConfigArgumentAnnotation(appHostConfig));
        }

        if (project.TryGetLastAnnotation<SiteArgumentAnnotation>(out var site) &&
            project.TryGetLastAnnotation<ConfigArgumentAnnotation>(out var cfg))
        {
            AddBindings(project, site, cfg);
        }

        return Task.CompletedTask;
    }

    // Apparently this is too late
    public Task ConfigureBeforeResourceStartedAsync()
    {
        //var appHostConfig = options.Value.ApplicationHostConfig!;
        //if (!project.HasAnnotationOfType<ConfigArgumentAnnotation>())
        //{
        //    project.Annotations.Add(new ConfigArgumentAnnotation(appHostConfig));
        //}

        //if (project.TryGetLastAnnotation<SiteArgumentAnnotation>(out var site) &&
        //    project.TryGetLastAnnotation<ConfigArgumentAnnotation>(out var cfg))
        //{
        //    AddBindings(project, site, cfg);
        //}
        return Task.CompletedTask;
    }

    public async Task ConfigureAfterEndpointsAllocatedAsync()
    {
        if (project.TryGetLastAnnotation<CertificateAnnotationBase>(out var cert) &&
            project.TryGetEndpoints(out var endpoints) &&
            endpoints.Any(ep => ep.UriScheme == "https"))
        {
            await ConfigureHttpsCertificate(cert, endpoints);
        }
    }

    private static readonly Guid netshappid = Guid.Parse("{214124cd-d05b-4309-9af9-9caa44b2b74a}");

    private async Task ConfigureHttpsCertificate(CertificateAnnotationBase cert, IEnumerable<EndpointAnnotation> endpoints)
    {
        var thumbprint = await cert.GetCertificateThumbprintAsync(serviceProvider);

        // TODO: Should we be clever here and try and snapshot the netsh state and restore when we're done?
        // This might be more difficult if we hard exit the apphost in some way.

        // TODO: Do a single elevation for all commands instead of one per command.
        foreach (var ep in endpoints.Where(ep => ep.UriScheme == "https"))
        {
            await ConfigureHttpsCertificate(cert, thumbprint, ep);
        }
    }

    private async Task ConfigureHttpsCertificate(CertificateAnnotationBase cert, string thumbprint, EndpointAnnotation ep)
    {
        var port = ep.TargetPort ?? ep.AllocatedEndpoint!.Port;
        var url = new UriBuilder(ep.AllocatedEndpoint!.UriString).ToString();

        var (_, sslcertjson) = await commandExecutor.GetCommandOutputAsync("netsh.exe", $"http show sslcert ipport=0.0.0.0:{port} json=enable", false);
        var sslcert = JsonSerializer.Deserialize<NetshSslCert>(sslcertjson);

        if (sslcert is not null &&
            sslcert.SslCertificateBindings.Length == 1 &&
            sslcert.SslCertificateBindings[0].Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase) &&
            sslcert.SslCertificateBindings[0].SslCertStoreName.Equals(cert.StoreName, StringComparison.OrdinalIgnoreCase) &&
            sslcert.SslCertificateBindings[0].AppId.Equals(netshappid))
        {
            logger.LogInformation("Certificate {Thumbprint} already bound to port {Port}", thumbprint, port);
            // We assume that if the port is bound to the dev certificate, the urlacl will also be set correctly.
        }
        else
        {
            if (sslcert?.SslCertificateBindings.Length > 0)
            {
                logger.LogWarning("Certificate {Thumbprint} already bound to port {Port} with different settings", thumbprint, port);
                await commandExecutor.ExecuteAdminCommandAsync("netsh.exe", "http", "delete", "sslcert", $"ipport=0.0.0.0:{port}");
            }
            logger.LogInformation("Binding certificate {Thumbprint} to port {Port}", thumbprint, port);
            await commandExecutor.ExecuteAdminCommandAsync("netsh.exe", "http", "add", "sslcert", $"ipport=0.0.0.0:{port}",
                $"\"appid={netshappid:B}\"", $"certhash={thumbprint}", $"certstorename={cert.StoreName}");

            // Since we can't check if the urlacl is already set, we just delete and re-add it.
            logger.LogInformation("Removing ACL for {url}", url);
            await commandExecutor.ExecuteAdminCommandAsync("netsh.exe", "http", "delete", "urlacl", $"url={url}");
            logger.LogInformation("Setting ACL for {url} to everyone", url);
            await commandExecutor.ExecuteAdminCommandAsync("netsh.exe", "http", "add", "urlacl", $"url={url}", "user=everyone");
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
            foreach (var binding in siteConfig.Bindings)
            {
                AddBinding(project, binding);
            }
        }
        else
        {
            logger.LogError("Site {Site} not found in {AppHostConfig}", site.Site, cfg.ApplicationHostConfig);
        }
    }

    private void AddBinding(IISExpressProjectResource project, Binding binding)
    {
        var endpoint = project.Annotations
                            .OfType<EndpointAnnotation>()
                            .Where(ea => StringComparer.OrdinalIgnoreCase.Equals(ea.Name, binding.Protocol))
                            .SingleOrDefault();

        if (endpoint is null)
        {
            endpoint = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, name: binding.Protocol);
            project.Annotations.Add(endpoint);
            logger.LogInformation("Adding endpoint {Endpoint} -> {BindingInformation} to {Resource} from applicationHost.config", endpoint.Name, binding.BindingInformation, project.Name);
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
