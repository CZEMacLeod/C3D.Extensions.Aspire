using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Resources;

public class IISExpressSiteResource(IISExpressResource iisExpress, string siteName, string workingDirectory)
    : ExecutableResource(siteName, iisExpress.Path, workingDirectory), IResourceWithServiceDiscovery, IResourceWithEnvironment
{
    public IISExpressResource IISExpress => iisExpress;
}
