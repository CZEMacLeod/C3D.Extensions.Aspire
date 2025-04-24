using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Resources;

public class IISExpressProjectResource(string name, string path, string workingDirectory)
    : ExecutableResource(name, path, workingDirectory), IResourceWithServiceDiscovery
{
}

public class IISExpressSiteResource(IISExpressResource iisExpress, string siteName, string workingDirectory) : IResourceWithServiceDiscovery, IResourceWithParent
{
    IResource IResourceWithParent.Parent => iisExpress;

    public IISExpressResource Parent => iisExpress;

    public string WorkingDirectory => workingDirectory;

    public string Name => siteName;

    public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();
}

public class IISExpressResource(string name, string path, IISExpressBitness bitness) : ExecutableResource(name, path, System.IO.Directory.CreateTempSubdirectory("aspire.iisexpress").FullName)
{
    public IISExpressBitness Bitness => bitness;

    public string Directory { get; } = Path.GetDirectoryName(path)!;

    public ICollection<IISExpressSiteResource> Projects { get; } = [];
}
