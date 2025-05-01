using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.VisualStudioDebug;

public interface IDebugBuilder<TResource> : IResourceBuilder<TResource>
    where TResource : IResource
{
    public IResourceBuilder<TResource> ResourceBuilder { get; }

    public bool IsDebugMode { get; }
}
