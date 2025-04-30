using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Configuration;

namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class SiteConfigurationAnnotation(Action<Site> configure) : IResourceAnnotation
{
    public void Configure(Site site) => configure(site);
}
