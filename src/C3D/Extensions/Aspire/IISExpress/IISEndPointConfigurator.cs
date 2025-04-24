using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

internal class IISEndPointConfigurator
{
    private readonly IOptions<IISExpressOptions> options;
    private readonly ILogger<IISEndPointConfigurator> logger;
    private readonly IISExpressProjectResource project;

    public IISEndPointConfigurator(
        IOptions<IISExpressOptions> options,
        ILogger<IISEndPointConfigurator> logger,
        IISExpressProjectResource project)
    {
        this.options = options;
        this.logger = logger;
        this.project = project;
    }

    public async Task ConfigureBeforeResourceStartedAsync()
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
    }

    public async Task ConfigureAfterEndpointsAllocatedAsync()
    {
        if (project.TryGetLastAnnotation<CertificateAnnotation>(out var cert) &&
            project.TryGetEndpoints(out var endpoints) &&
            endpoints.Any(ep => ep.UriScheme == "https"))
        {
            await ConfigureHttps(cert, endpoints);
        }
    }

    private static readonly Guid netshappid = Guid.Parse("{214124cd-d05b-4309-9af9-9caa44b2b74a}");

    private async Task ConfigureHttps(CertificateAnnotation cert, IEnumerable<EndpointAnnotation> endpoints)
    {
        var thumbprint = await cert.GetCertificateThumbprintAsync(logger);

        // TODO: Should we be clever here and try and snapshot the netsh state and restore when we're done?
        // This might be more difficult if we hard exit the apphost in some way.

        var isAdmin = IISEndPointConfigurator.IsAdministrator();
        // TODO: Do a single elevation for all commands instead of one per command.
        foreach (var ep in endpoints.Where(ep => ep.UriScheme == "https"))
        {
            var port = ep.TargetPort ?? ep.AllocatedEndpoint!.Port;
            var url = ep.AllocatedEndpoint!.UriString;

            var sslcertjson = await GetCommandOutputAsync("netsh.exe", $"http show sslcert ipport=0.0.0.0:{port} json=enable", false);
            var sslcert = JsonSerializer.Deserialize<NetshSslCert>(sslcertjson);

            // TODO: Apparently netsh doesn't have a json outfor for urlacl, so we can't check if the urlacl is already set.
            // var urlacljson = await GetCommandOutputAsync("netsh.exe", $"http show urlacl url={url} json=enable", false);

            if (sslcert is not null &&
                sslcert.SslCertificateBindings.Length == 1 &&
                sslcert.SslCertificateBindings[0].SslHash.Equals(thumbprint, StringComparison.OrdinalIgnoreCase) &&
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
                    await ExecuteCommandAsync("netsh.exe", $"http delete sslcert ipport=0.0.0.0:{port}", !isAdmin);
                }
                logger.LogInformation("Binding certificate {Thumbprint} to port {Port}", thumbprint, port);
                await ExecuteCommandAsync("netsh.exe", $"http add sslcert ipport=0.0.0.0:{port} \"appid={netshappid}\" certhash={thumbprint} certstorename={cert.StoreLocation}", !isAdmin);

                // Since we can't check if the urlacl is already set, we just delete and re-add it.
                logger.LogInformation("Removing ACL for {url}", url);
                await ExecuteCommandAsync("netsh.exe", $"http delete urlacl url={url}", !isAdmin);
                logger.LogInformation("Setting ACL for {url} to everyone", url);
                await ExecuteCommandAsync("netsh.exe", $"http add urlacl url={url} user=everyone", !isAdmin);
            }
        }
    }

    private static bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        if (identity == null)
        {
            return false;
        }
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private async Task ExecuteCommandAsync(string cmd, string args, bool runAs)
    {
        var proc = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (runAs)
        {
            proc.Verb = "runas";
        }
        logger.LogInformation("Running command: {Command} {Args}", cmd, args);
        using var process = new Process { StartInfo = proc };
        process.OutputDataReceived += (sender, e) => logger.LogInformation(e.Data);
        process.ErrorDataReceived += (sender, e) => logger.LogError(e.Data);
        process.Start();
        await process.WaitForExitAsync();
    }

    private async Task<string> GetCommandOutputAsync(string cmd, string args, bool runAs)
    {
        logger.LogInformation("Running command: {Command} {Args}", cmd, args); 
        var proc = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (runAs)
        {
            proc.Verb = "runas";
        }
        using var process = new Process { StartInfo = proc };
        process.ErrorDataReceived += (sender, e) => logger.LogError(e.Data);
        process.Start();
        await process.WaitForExitAsync();
        return await process.StandardOutput.ReadToEndAsync();
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
