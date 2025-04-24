using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Resources;

public class IISExpressResource(string name, string path, IISExpressBitness bitness) : IResource
{
    public IISExpressBitness Bitness => bitness;

    public string Directory { get; } = System.IO.Path.GetDirectoryName(path)!;

    public string Path => path;

    internal string TempDirectory { get; } = System.IO.Directory.CreateTempSubdirectory("aspire.iisexpress.").FullName;

    public ICollection<IISExpressSiteResource> Sites { get; } = [];

    public string Name => name;

    public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();
}
