using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    private void AddBindings(IISExpressProjectResource project, SiteArgumentAnnotation site, ConfigArgumentAnnotation cfg)
    { 
        var xdts = project.Annotations.OfType<ApplicationHostXdtAnnotation>().ToList();

        var appConfig = cfg.LoadConfiguration();
        var existingConfig = appConfig is not null;
        if (appConfig is null)
        {
            logger.LogWarning("Could not load application host config {AppHostConfig}", cfg.ApplicationHostConfig);
            logger.LogInformation("Creating {AppHostConfig} from template", cfg.ApplicationHostConfig);
            appConfig = project.Bitness.GetDefaultConfiguration();
            SaveConfig();
        }

        void SaveConfig()
        {
            if (existingConfig)
            {
                cfg = project.WithTemporaryConfiguration(appConfig, logger, xdts);
                logger.LogDebug("Using temp configuration {AppHostConfig}", cfg.ApplicationHostConfig);
                existingConfig = false;
            }
            else
            {
                cfg.SaveConfiguration(appConfig, logger, xdts);
            }
        }

        var siteConfig = appConfig.GetSite(site.Site);
        if (!project.TryGetLastAnnotation<AppPoolArgumentAnnotation>(out var appPool))
        {
            appPool = new AppPoolArgumentAnnotation(siteConfig?.Application.ApplicationPool ?? AppPoolArgumentAnnotation.DefaultAppPool);
            project.Annotations.Add(appPool);
        }
        if (!appConfig.SystemApplicationHost.ApplicationPools.Add
            .Any(a => string.Equals(a.Name, appPool.AppPool, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning("Application pool {AppPool} not found in {AppHostConfig}", appPool.AppPool, cfg.ApplicationHostConfig);
            // <add name="Clr4IntegratedAppPool" managedRuntimeVersion="v4.0" managedPipelineMode="Integrated" CLRConfigFile="%IIS_USER_HOME%\config\aspnet.config" autoStart="true" />
            appConfig.SystemApplicationHost.ApplicationPools.Add.Add(appPool.CreateIISConfigApplicationPool());
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
                    siteConfig.Application.VirtualDirectory.PhysicalPath = projectPath;
                    SaveConfig();
                }
            }
            
            var matches = new Dictionary<EndpointAnnotation, Configuration.Binding>();
            foreach (var binding in siteConfig.Bindings.Binding)
            {
                var endpoint = project.AddOrUpdateEndpointFromBinding(binding);
                matches[endpoint] = binding;
            }

            var endpoints = project.Annotations.OfType<EndpointAnnotation>().Except(matches.Keys).ToList();
            if (endpoints.Count != 0)
            {
                siteConfig.Bindings.Binding.AddRange(endpoints.Select(e => e.CreateIISConfigBinding()));
                SaveConfig();
            }

            project.ShowIISExpressHttpsEndpointInformation(eventing, logger, project.Bitness);
        }
        else
        {
            logger.LogWarning("Site {Site} not found in {AppHostConfig}", site.Site, cfg.ApplicationHostConfig);

            if (!project.TryGetEndpoints(out var endpoints) || !endpoints.Any())
            {
                logger.LogWarning("No endpoints found for project {Project}", project.Name);
                project.WithDefaultIISExpressEndpoints();
                endpoints = project.Annotations.OfType<EndpointAnnotation>();
            }

            var siteId = appConfig.SystemApplicationHost.Sites.Site.Max(s => int.TryParse(s.Id, out var id) ? id : 0) + 1;
            var newSite = project.CreateIISConfigSite(siteId, site.Site, project.WorkingDirectory, appPool.AppPool, logger);
            appConfig.SystemApplicationHost.Sites.Site.Add(newSite);
            SaveConfig();

            project.ShowIISExpressHttpsEndpointInformation(eventing, logger, project.Bitness);
        }
    }
}
