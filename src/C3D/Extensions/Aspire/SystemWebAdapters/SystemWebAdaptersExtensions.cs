using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Resources;
using C3D.Extensions.Aspire.SystemWebAdapters.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class SystemWebAdaptersExtensions
{
    public static IResourceBuilder<SystemWebAdaptersResource> AddSystemWebAdapters(this IDistributedApplicationBuilder builder,
        [ResourceName] string resourceName,
        string? envNameApiKey = "RemoteApp__ApiKey",
        string? envNameUrl = "RemoteApp__RemoteAppUrl",
        Guid? key = null) => builder.AddSystemWebAdapters(resourceName,
            builder.AddParameter($"{resourceName}-key", new GuidParameterDefault(key), persist: true),
            envNameApiKey, envNameUrl);

    public static IResourceBuilder<SystemWebAdaptersResource> AddSystemWebAdapters(this IDistributedApplicationBuilder builder,
        [ResourceName] string resourceName,
        IResourceBuilder<ParameterResource> key,
        string? envNameApiKey = "RemoteApp__ApiKey",
        string? envNameUrl = "RemoteApp__RemoteAppUrl") => builder.AddSystemWebAdapters(resourceName, key.Resource, envNameApiKey, envNameUrl);

    public static IResourceBuilder<SystemWebAdaptersResource> AddSystemWebAdapters(this IDistributedApplicationBuilder builder,
        [ResourceName] string resourceName,
        ParameterResource key,
        string? envNameApiKey = "RemoteApp__ApiKey",
        string? envNameUrl = "RemoteApp__RemoteAppUrl")
    {
        var resource = new SystemWebAdaptersResource(resourceName, key);

        builder.Eventing.Subscribe<AfterResourcesCreatedEvent>(async (@event, token) =>
        {
            var notifications = @event.Services.GetRequiredService<ResourceNotificationService>();
            var logger = @event.Services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);
            await notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Starting });
            logger.LogInformation("Starting System Web Adapters");
            logger.LogInformation("Framework: {Framework}", resource.Framework?.Name);
            logger.LogInformation("Core: {Core}", resource.Core?.Name);
            if (resource.Framework is null || resource.Core is null)
            {
                logger.LogError("Framework or Core is null");
                await notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.FailedToStart });
                return;
            }
            logger.LogInformation("Waiting for framework {Framework} to start", resource.Framework.Name);
            await notifications.WaitForResourceAsync(resource.Framework.Name, KnownResourceStates.Running, token);
            logger.LogInformation("Waiting for core {Core} to start", resource.Core.Name);
            await notifications.WaitForResourceAsync(resource.Core.Name, KnownResourceStates.Running, token);
            logger.LogInformation("System Web Adapters started");
            await notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Running });
        });

        return builder
            .AddResource(resource)
            .WithAnnotation(new SystemWebAdaptersAnnotation(envNameApiKey, envNameUrl));
    }

    public static IResourceBuilder<SystemWebAdaptersResource> WithFramework(this IResourceBuilder<SystemWebAdaptersResource> resourceBuilder,
        IResourceBuilder<IISExpressProjectResource> iisExpressProjectResource,
        string? envNameKey = null,
        string endpoint = "http") =>
        resourceBuilder.WithFrameworkInner(iisExpressProjectResource, envNameKey, endpoint);

    public static IResourceBuilder<SystemWebAdaptersResource> WithFramework(this IResourceBuilder<SystemWebAdaptersResource> resourceBuilder,
        IResourceBuilder<IISExpressSiteResource> iisExpressProjectResource,
        string? envNameKey = null,
        string endpoint = "http") =>
        resourceBuilder.WithFrameworkInner(iisExpressProjectResource, envNameKey, endpoint);

    private static IResourceBuilder<SystemWebAdaptersResource> WithFrameworkInner<T>(this IResourceBuilder<SystemWebAdaptersResource> resourceBuilder,
        IResourceBuilder<T> iisExpressProjectResource,
        string? envNameKey = null,
        string endpoint = "http")
        where T : IResourceWithEndpoints, IResourceWithEnvironment
    {
        if (iisExpressProjectResource.TryGetSystemWebAdapters(out var _))
        {
            throw new InvalidOperationException($"The resource {iisExpressProjectResource.Resource.Name} already has an associated SystemWebAdapters resource.");
        }
        resourceBuilder.WithRelationship(iisExpressProjectResource.Resource, "framework");
        resourceBuilder.Resource.Framework = iisExpressProjectResource.Resource;
        resourceBuilder.Resource.FrameworkEndpointName = endpoint;

        if (resourceBuilder.Resource.TryGetLastAnnotation<SystemWebAdaptersAnnotation>(out var swa))
        {
            envNameKey ??= swa.EnvNameKey;
        }
        iisExpressProjectResource
            .WithRelationship(resourceBuilder.Resource, "SWA")
            .WithAnnotation(new SystemWebAdaptersAnnotation(envNameKey, null))
            .WithSystemWebAdaptersEnvironment();

        if (resourceBuilder.Resource.Core is not null)
        {
            var coreBuilder = resourceBuilder.ApplicationBuilder.CreateResourceBuilder(resourceBuilder.Resource.Core);
            coreBuilder.WithRelationship(iisExpressProjectResource.Resource, "SWA-Framework");
        }
        return resourceBuilder;
    }

    public static IResourceBuilder<SystemWebAdaptersResource> WithCore(this IResourceBuilder<SystemWebAdaptersResource> resourceBuilder,
        IResourceBuilder<IResourceWithEnvironment> coreProjectResource,
        string? envNameKey = null,
        string? envNameUrl = null)
    {
        if (coreProjectResource.TryGetSystemWebAdapters(out var _))
        {
            throw new InvalidOperationException($"The resource {coreProjectResource.Resource.Name} already has an associated SystemWebAdapters resource.");
        }

        resourceBuilder.Resource.Core = coreProjectResource.Resource;
        resourceBuilder.WithRelationship(coreProjectResource.Resource, "core");
        if (resourceBuilder.Resource.TryGetLastAnnotation<SystemWebAdaptersAnnotation>(out var swa))
        {
            envNameKey ??= swa.EnvNameKey;
            envNameUrl ??= swa.EnvNameUrl;
        }
        coreProjectResource
            .WithRelationship(resourceBuilder.Resource, "SWA")
            .WithAnnotation(new SystemWebAdaptersAnnotation(envNameKey, envNameUrl))
            .WithSystemWebAdaptersEnvironment();

        if (resourceBuilder.Resource.Framework is not null)
        {
            coreProjectResource
                .WithRelationship(resourceBuilder.Resource.Framework, "SWA-Framework");
        }
        return resourceBuilder;
    }

    private static bool TryGetSystemWebAdapters<T>(this IResourceBuilder<T> resourceBuilder, [NotNullWhen(true)] out SystemWebAdaptersResource? systemWebAdaptersResource)
        where T : IResourceWithEnvironment
        => resourceBuilder.Resource.TryGetSystemWebAdapters(out systemWebAdaptersResource);

    private static bool TryGetSystemWebAdapters<T>(this T resource, [NotNullWhen(true)] out SystemWebAdaptersResource? systemWebAdaptersResource)
        where T : IResource
    {
        if (resource is SystemWebAdaptersResource swa)
        {
            systemWebAdaptersResource = swa;
            return true;
        }
        if (resource.TryGetAnnotationsOfType<ResourceRelationshipAnnotation>(out var relationships) &&
            relationships.LastOrDefault(r => r.Type == "SWA")?.Resource is SystemWebAdaptersResource swar)
        {
            systemWebAdaptersResource = swar;
            return true;
        }
        systemWebAdaptersResource = null;
        return false;
    }

    private static IResourceBuilder<T> WithSystemWebAdaptersEnvironment<T>(this IResourceBuilder<T> resourceBuilder)
        where T : IResourceWithEnvironment => resourceBuilder.WithEnvironment(c =>
        {
            if (c.Resource.TryGetLastAnnotation<SystemWebAdaptersAnnotation>(out var swa) &&
                c.Resource.TryGetSystemWebAdapters(out var swar))
            {

                if (!string.IsNullOrEmpty(swa.EnvNameKey))
                {
                    c.EnvironmentVariables[swa.EnvNameKey] = swar.KeyParameter;
                }
                if (!string.IsNullOrEmpty(swa.EnvNameUrl))
                {
                    c.EnvironmentVariables[swa.EnvNameUrl] = swar.FrameworkEndpoint ?? throw new InvalidOperationException($"The framework endpoint is not set."); ;
                }
            }
        });

    [Obsolete("Use AddSystemWebAdapters().WithFramework().WithCore() instead.")]
    public static IResourceBuilder<IISExpressProjectResource> WithSystemWebAdapters(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
            string envNameBase = "RemoteApp",
            string envNameApiKey = "__ApiKey",
            string envNameUrl = "__RemoteAppUrl",
            Guid? key = null) => resourceBuilder.WithSystemWebAdaptersFrameworkInner(envNameBase + envNameApiKey, envNameBase + envNameUrl, key);

    [Obsolete("Use AddSystemWebAdapters().WithFramework().WithCore() instead.")]
    public static IResourceBuilder<IISExpressSiteResource> WithSystemWebAdapters(this IResourceBuilder<IISExpressSiteResource> resourceBuilder,
        string envNameBase = "RemoteApp",
        string envNameApiKey = "__ApiKey",
        string envNameUrl = "__RemoteAppUrl",
        Guid? key = null) => resourceBuilder.WithSystemWebAdaptersFrameworkInner(envNameBase + envNameApiKey, envNameBase + envNameUrl, key);

    [Obsolete("Use AddSystemWebAdapters().WithFramework().WithCore() instead.")]
    private static IResourceBuilder<T> WithSystemWebAdaptersFrameworkInner<T>(this IResourceBuilder<T> resourceBuilder,
        string? envNameKey = null,
        string? envNameUrl = null,
        Guid? key = null)
        where T : IResourceWithEndpoints, IResourceWithEnvironment
    {
        if (resourceBuilder.TryGetSystemWebAdapters(out var swar))
        {
            resourceBuilder.WithAnnotation(new SystemWebAdaptersAnnotation(envNameKey, envNameUrl));

            swar.KeyParameter.Default = new GuidParameterDefault(key);
            swar.Framework = resourceBuilder.Resource;
            resourceBuilder.ApplicationBuilder.CreateResourceBuilder(swar)
                .WithAnnotation(new SystemWebAdaptersAnnotation(envNameKey, envNameUrl));
        }
        else
        {
            resourceBuilder.ApplicationBuilder
                .AddSystemWebAdapters(resourceBuilder.Resource.Name + "-swa",
                    envNameKey,
                    envNameUrl,
                    key)
                .WithFrameworkInner(resourceBuilder);
        }
        return resourceBuilder;
    }

    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        string envNameKey,
        string envNameUrl)
        => resourceBuilder.WithAnnotation(new SystemWebAdaptersAnnotation(envNameKey, envNameUrl));

    [Obsolete("Use AddSystemWebAdapters().WithFramework().WithCore() instead.")]
    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        IResourceBuilder<IISExpressProjectResource> iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http")
        => resourceBuilder.WithSystemWebAdaptersCoreInner(iisExpressResource, envNameKey, envNameUrl, endpoint);

    [Obsolete("Use AddSystemWebAdapters().WithFramework().WithCore() instead.")]
    public static IResourceBuilder<ProjectResource> WithSystemWebAdapters(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        IResourceBuilder<IISExpressSiteResource> iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http")
        => resourceBuilder.WithSystemWebAdaptersCoreInner(iisExpressResource, envNameKey, envNameUrl, endpoint);

    [Obsolete("Use AddSystemWebAdapters().WithFramework().WithCore() instead.")]
    private static IResourceBuilder<ProjectResource> WithSystemWebAdaptersCoreInner<T>(
        this IResourceBuilder<ProjectResource> resourceBuilder,
        IResourceBuilder<T> iisExpressResource,
        string? envNameKey = null,
        string? envNameUrl = null,
        string endpoint = "http")
        where T : IResourceWithEndpoints, IResourceWithEnvironment
    {
        if (resourceBuilder.TryGetSystemWebAdapters(out var swar))
        {
            resourceBuilder.WithAnnotation(new SystemWebAdaptersAnnotation(envNameKey, envNameUrl));
            swar.Core = resourceBuilder.Resource;
            swar.FrameworkEndpointName = endpoint;
        }
        else if (iisExpressResource.TryGetSystemWebAdapters(out swar))
        {
            swar.FrameworkEndpointName = endpoint;
            resourceBuilder.ApplicationBuilder.CreateResourceBuilder(swar)
                .WithCore(resourceBuilder, envNameKey, envNameUrl);
        }
        else
        {
            resourceBuilder.ApplicationBuilder
                .AddSystemWebAdapters(resourceBuilder.Resource.Name + "-swa",
                    envNameKey ?? "RemoteApp__ApiKey",
                    envNameUrl ?? "RemoteApp__RemoteAppUrl",
                    null)
                .WithCore(resourceBuilder);
        }
        return resourceBuilder;
    }

    private class GuidParameterDefault : ParameterDefault
    {
        private Guid? key;

        public GuidParameterDefault(Guid? key) => this.key = key;

        public override string GetDefaultValue() => (key ??= Guid.NewGuid()).ToString();

        public override void WriteToManifest(ManifestPublishingContext context)
        {
            context.Writer.WriteString("value", GetDefaultValue());
        }
    }
}