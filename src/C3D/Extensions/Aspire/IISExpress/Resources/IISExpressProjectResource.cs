using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Annotations;

namespace C3D.Extensions.Aspire.IISExpress.Resources;

public class IISExpressProjectResource(string name, string path, string workingDirectory)
    : ExecutableResource(name, path, workingDirectory), IResourceWithServiceDiscovery
{
    public IISExpressProjectResource(string name, string path, string workingDirectory, IISExpressBitness bitness) :
        this(name, path, workingDirectory)
    {
        Annotations.Add(new IISExpressBitnessAnnotation(bitness));
    }

    public IISExpressBitness Bitness => this.Annotations.OfType<IISExpressBitnessAnnotation>().Single().Bitness;
}
