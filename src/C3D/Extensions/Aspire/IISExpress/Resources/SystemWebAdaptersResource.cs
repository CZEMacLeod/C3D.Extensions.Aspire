using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Resources;

/// <summary>
/// Represents a systemweb-adapters resource. This is a special resource that is used to contain the shared key used by systemweb-adapters.
/// </summary>
public class SystemWebAdaptersResource : IResource, IResourceWithoutLifetime
{
    private EndpointReference? frameworkEndpoint = null;

    public string Name { get; private set; }

    public SystemWebAdaptersResource(string name, ParameterResource key)
    {
        Name = name;
        KeyParameter = key;
    }

    public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();

    /// <summary>
    /// Gets or sets the core resource. This is the resource that contains the aspnetcore project.
    /// </summary>
    public IResourceWithEnvironment? Core { get; internal set; } = null;

    /// <summary>
    /// Gets or sets the core resource. This is the resource that contains the asp.net v4.x framework project.
    /// </summary>
    public IResourceWithEndpoints? Framework { get; internal set; } = null;

    /// <summary>
    /// Gets or sets the framework endpoint. This is the endpoint that is used by the aspnetcore project to communicate with the framework project.
    /// </summary>
    public EndpointReference? FrameworkEndpoint
    {
        get => frameworkEndpoint ??= string.IsNullOrEmpty(FrameworkEndpointName) ? null : Framework?.GetEndpoint(FrameworkEndpointName);
        internal set
        {
            Framework = value?.Resource;
            FrameworkEndpointName = value?.EndpointName;
            frameworkEndpoint = value;
        }
    }

    /// <summary>
    /// Gets or sets the framework endpoint name. This is the name of the endpoint that is used by the aspnetcore project to communicate with the framework project.
    /// </summary>
    public string? FrameworkEndpointName { get; internal set; }

    /// <summary>
    /// Gets the parameter that contains the systemweb-adapters common key.
    /// </summary>
    public ParameterResource KeyParameter { get; private set; }

}
