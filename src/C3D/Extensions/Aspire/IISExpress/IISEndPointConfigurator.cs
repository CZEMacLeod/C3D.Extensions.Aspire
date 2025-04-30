using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Models.LaunchSettings;
using C3D.Extensions.Aspire.IISExpress.Resources;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

internal class IISEndPointConfigurator
{
    private readonly DistributedApplicationModel appModel;
    private readonly IDistributedApplicationEventing eventing;
    private readonly IOptions<IISExpressOptions> options;
    private readonly ILogger<IISEndPointConfigurator> logger;

    public IISEndPointConfigurator(DistributedApplicationModel appModel,
        IDistributedApplicationEventing eventing,
        IOptions<IISExpressOptions> options,
        ILogger<IISEndPointConfigurator> logger)
    {
        this.appModel = appModel;
        this.eventing = eventing;
        this.options = options;
        this.logger = logger;
    }

    public void Configure()
    {
        var appHostConfig = options.Value.ApplicationHostConfig!;
        var projects = appModel.Resources.OfType<IISExpressProjectResource>().ToList();
        foreach (var project in projects)
        {
            if (!project.HasAnnotationOfType<ConfigArgumentAnnotation>())
            {
                project.Annotations.Add(new ConfigArgumentAnnotation(appHostConfig));
            }

            Dictionary<string, LaunchProfile>? launchProfiles = null;
            if (project.TryGetLastAnnotation<IISProfileSettingsAnnotation>(out var iisProfileSettings))
            {
                launchProfiles = ConfigureIISProfileSettings(project);
            }

            if (project.TryGetLastAnnotation<LaunchProfileAnnotation>(out var launchProfile))
            {
                ConfigureLaunchProfile(project, launchProfile.LaunchProfileName, launchProfiles);
            }

            if (project.TryGetLastAnnotation<SiteArgumentAnnotation>(out var site) &&
                project.TryGetLastAnnotation<ConfigArgumentAnnotation>(out var cfg))
            {
                AddBindings(project, site, cfg);
            }
        }
    }

    private void ConfigureLaunchProfile(IISExpressProjectResource project, string launchProfileName, Dictionary<string, LaunchProfile>? launchProfiles)
    {
        launchProfiles ??= GetIISLaunchSettings(project)?.Profiles;
        if (launchProfiles is null)
        {
            logger.LogWarning("LaunchSettings.json could not be loaded");
            return;
        }

        if (!launchProfiles.TryGetValue(launchProfileName, out var launchProfile))
        {
            logger.LogWarning("Launch profile {LaunchProfileName} not found in LaunchSettings.json", launchProfileName);
            return;
        }

        var launchUrls = ConfigureLaunchUrls(project, launchProfile);
        ConfigureLaunchBrowser(project, launchProfile, launchUrls);

        // ConfigureEnvironmentVariables
        project.Annotations.Add(new EnvironmentCallbackAnnotation(c =>
        {
            foreach (var ev in launchProfile.EnvironmentVariables)
            {
                c.EnvironmentVariables.TryAdd(ev.Key, ev.Value);
            }
        }));
    }

    private void ConfigureLaunchBrowser(IISExpressProjectResource project, LaunchProfile launchProfile, List<string> launchUrls)
    {
        if (launchProfile.LaunchBrowser.GetValueOrDefault(false))
        {
            var edge = new ExecutableResource(project.Name + "-launchurl",
                Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft", "Edge", "Application", "msedge.exe"), project.WorkingDirectory);
            if (project.HasAnnotationOfType<DebugAttachAnnotation>())
            {
                var debug = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, "debug")
                {
                    UriScheme = "ws",
                    IsExternal = false
                };
            edge.Annotations.Add(debug);
            }

            var userDataDir = System.IO.Directory.CreateTempSubdirectory("aspire.msedge.userdata").FullName;
            var profileDir = System.IO.Directory.CreateTempSubdirectory("aspire.msedge.profile").FullName;
            edge.Annotations.Add(
                    new CommandLineArgsCallbackAnnotation(c =>
                    {
                        c.Add("--new-window");
                        if (launchUrls.Count == 0)
                        {
                            c.Add(project.GetEndpoint("https")?.Url ?? project.GetEndpoint("https")?.Url ?? "about:blank");
                        }
                        else
                        {
                            foreach (var url in launchUrls)
                            {
                                c.Add(url);
                            }
                        }
                        c.Add($"--user-data-dir={userDataDir}");
                        c.Add($"--profile-directory={profileDir}");
                        c.Add("--no-first-run");
                        c.Add("--no-sandbox");
                        if (project.HasAnnotationOfType<DebugAttachAnnotation>())
                        {
                            c.Add(ReferenceExpression.Create($"--remote-debugging-port={edge.GetEndpoint("debug").Property(EndpointProperty.Port)}"));
                            c.Add("--wait-for-debugger");
                        }
                    }));
            appModel.Resources.Add(edge);

            if (project.HasAnnotationOfType<DebugAttachAnnotation>())
            {
                edge.Annotations.Add(new DebugAttachAnnotation() { DebugMode = VisualStudioDebug.DebugMode.VisualStudio, Skip = true });
                edge.Annotations.Add(new DebugAttachEngineAnnotation() { Engine = VisualStudioDebug.WellKnown.Engines.JavaScript });
                eventing.Subscribe<BeforeResourceStartedEvent>((@event, c) =>
                {
                    if (@event.Resource.TryGetEndpoints(out var endpoints))
                    {
                        var debugEndpoint = endpoints.SingleOrDefault(ea => ea.Name == "debug");
                        if (debugEndpoint is null)
                        {
                            logger.LogError("Debug endpoint not found");
                            if (edge.TryGetLastAnnotation<DebugAttachAnnotation>(out var debugAttach))
                            {
                                debugAttach.Skip = true;
                            }
                        }
                        else
                        {
                            logger.LogInformation("Adding DebugAttachTransportAnnotation to {ResourceName} {Uri}", @event.Resource.Name, debugEndpoint?.AllocatedEndpoint!.UriString);
                            @event.Resource.Annotations.Add(new DebugAttachTransportAnnotation()
                            {
                                Transport = VisualStudioDebug.WellKnown.Transports.V8Inspector,
                                Qualifier = debugEndpoint?.AllocatedEndpoint!.UriString
                            });
                            if (edge.TryGetLastAnnotation<DebugAttachAnnotation>(out var debugAttach))
                            {
                                //debugAttach.Skip = false;
                            }
                        }
                    }
                    return Task.CompletedTask;
                });
            }
        }
    }

    private List<string> ConfigureLaunchUrls(IISExpressProjectResource project, LaunchProfile launchProfile)
    {
        List<string> launchUrls = new();
        if (!string.IsNullOrEmpty(launchProfile.LaunchUrl))
        {
            var urls = launchProfile.LaunchUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var launchUrl in urls)
            {
                if (Uri.TryCreate(launchUrl, UriKind.Absolute, out var uri))
                {
                    if (project.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints))
                    {
                        var ep = endpoints.SingleOrDefault(ea => ea.UriScheme == uri.Scheme && ea.TargetPort == uri.Port);
                        if (ep is null)
                        {
                            logger.LogWarning("Project does not contain a matching endpoint for LaunchUrl {LaunchUrl}", launchProfile.LaunchUrl);
                        }
                        else
                        {
                            if (uri.Host == "*")
                            {
                                uri = (new UriBuilder(uri)
                                {
                                    Host = "localhost"
                                }).Uri;
                            }
                            project.Annotations.Add(new ResourceUrlAnnotation()
                            {
                                Url = uri.ToString(),
                                DisplayText = "Launch URL",
                                DisplayOrder = 100
                            });
                            launchUrls.Add(uri.ToString());
                        }
                    }
                    else
                    {
                        logger.LogWarning("Project does not contain a matching endpoint for LaunchUrl {LaunchUrl}", launchProfile.LaunchUrl);
                    }
                }
                else
                {
                    logger.LogWarning("LaunchSettings.json does not contain valid LaunchUrl");
                }
            }
        }
        return launchUrls;
    }

    private Dictionary<string, LaunchProfile>? ConfigureIISProfileSettings(IISExpressProjectResource project)
    {

        IISLaunchSettings? ls = GetIISLaunchSettings(project);

        if (ls is null)
        {
            logger.LogWarning("LaunchSettings.json could not be loaded");
            return null;
        }

        // TODO: Handle authentication settings
        var bindings = ls.IISSettings?.IISExpress;
        if (bindings is null)
        {
            logger.LogWarning("LaunchSettings.json does not contain IISExpress bindings");
            return null;
        }

        project.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints);

        if (!string.IsNullOrEmpty(bindings.ApplicationUrl))
        {
            if (Uri.TryCreate(bindings.ApplicationUrl, UriKind.Absolute, out var uri))
            {
                AddOrUpdateEndpointForUri(project, endpoints, uri);
            }
            else
            {
                logger.LogWarning("LaunchSettings.json does not contain valid ApplicationUrl");
            }
        }
        if (bindings.SSLPort is not null)
        {
            var uri = new Uri($"https://localhost:{bindings.SSLPort}");
            AddOrUpdateEndpointForUri(project, endpoints, uri);
        }
        return ls?.Profiles;
    }

    private IISLaunchSettings? GetIISLaunchSettings(IISExpressProjectResource project)
    {
        // TODO: Detect if project is VB or C#
        var propsFolder = System.IO.Path.Combine(project.WorkingDirectory, "Properties");
        if (!Directory.Exists(propsFolder))
        {
            propsFolder = System.IO.Path.Combine(project.WorkingDirectory, "My Project");
        }
        if (!Directory.Exists(propsFolder))
        {
            logger.LogWarning("Properties folder not found in {WorkingDirectory}", project.WorkingDirectory);
            return null;
        }
        var lsFile = Path.Combine(propsFolder, "launchSettings.json");
        if (!File.Exists(lsFile))
        {
            logger.LogWarning("LaunchSettings.json not found in {PropertiesFolder}", propsFolder);
            return null;
        }
        using var fs = File.OpenRead(lsFile);

        return JsonSerializer.Deserialize<IISLaunchSettings>(fs);
    }

    private static void AddOrUpdateEndpointForUri(IISExpressProjectResource project, IEnumerable<EndpointAnnotation>? endpoints, Uri uri)
    {
        var ep = endpoints?
            .SingleOrDefault(ea => StringComparer.OrdinalIgnoreCase.Equals(ea.Name, uri.Scheme) &&
                (ea.IsProxied ? (ea.TargetPort is null || ea.TargetPort == uri.Port)
                    : (ea.Port is null || ea.Port == uri.Port)));
        if (ep is null)
        {
            AddEndpointForUri(project, uri);
        }
        else
        {
            ep.TargetPort = uri.Port;
        }
    }

    private static void AddEndpointForUri(IISExpressProjectResource project, Uri uri)
    {
        var ep = new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, uri.Scheme)
        {
            TargetPort = uri.Port,
            Name = uri.Scheme,
        };
        project.Annotations.Add(ep);
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

        endpoint.TargetPort = binding.Port;
        endpoint.UriScheme = binding.Protocol;
        //endpoint.IsProxied = false;
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
