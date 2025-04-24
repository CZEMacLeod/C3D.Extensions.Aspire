using Aspire.Hosting.ApplicationModel;
using System.Security.Cryptography.X509Certificates;
namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public abstract class CertificateAnnotationBase : IResourceAnnotation, IValueProvider
{
    protected readonly StoreLocation storeLocation;

    public CertificateAnnotationBase(string storeName = "My", string storeLocation = "CurrentUser")
    {
        StoreName = storeName;
        StoreLocation = storeLocation;
        if (Enum.TryParse<StoreLocation>(StoreLocation, out var sl))
        {
            this.storeLocation = sl;
        }
        else
        {
            throw new ArgumentException($"Invalid store location: {StoreLocation}");
        }
    }

    public virtual string StoreName { get; }
    public virtual string StoreLocation { get; }

    public virtual async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) => await GetCertificateThumbprintAsync(null);

    public abstract Task<string> GetCertificateThumbprintAsync(IServiceProvider? serviceProvider);

}
