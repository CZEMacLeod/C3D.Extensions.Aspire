using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class SystemWebAdaptersAnnotation : IResourceAnnotation
{
    public SystemWebAdaptersAnnotation(string? envNameKey, string? envNameUrl)
    {
        EnvNameKey = envNameKey;
        EnvNameUrl = envNameUrl;
    }

    public string? EnvNameKey { get; }
    public string? EnvNameUrl { get; }
}
