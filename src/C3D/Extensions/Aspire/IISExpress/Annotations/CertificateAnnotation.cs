using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class CertificateAnnotation : IResourceAnnotation, IValueProvider
{
    public CertificateAnnotation(string certificateHash, string storeName = "My", string storeLocation = "CurrentUser")
    {
        CertificateHash = certificateHash;
        StoreName = storeName;
        StoreLocation = storeLocation;
    }

    private string CertificateHash { get; set; }
    public virtual string StoreName { get; }
    public virtual string StoreLocation { get; }

    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ToString());

    public virtual Task<string> GetCertificateThumbprintAsync(ILogger logger)
    {
        if (Enum.TryParse<StoreLocation>(StoreLocation, out var storeLocation))
        {
            using var store = new X509Store(StoreName, storeLocation, OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, CertificateHash, true);
            if (certs.Count == 0)
            {
                throw new InvalidOperationException($"Valid certificate with thumbprint {CertificateHash} not found in store {StoreName}");
            }
            return Task.FromResult(certs[0].Thumbprint);
        }
        else
        {
            throw new ArgumentException($"Invalid store location: {StoreLocation}");
        }
    }
}

public class DevCertificateAnnotation : CertificateAnnotation
{
    private readonly IConfiguration configuration;

    public DevCertificateAnnotation(IConfiguration configuration, string storeName = "My", string storeLocation = "CurrentUser")
        : base(null!, storeName, storeLocation)
    {
        this.configuration = configuration;
    }
    public override async Task<string> GetCertificateThumbprintAsync(ILogger logger)
    {
        var (exported, certPath, certKeyPath) = await DevCertHostingExtensions.TryExportDevCertificateAsync(configuration, logger);

        if (!exported)
        {
            throw new InvalidOperationException("Failed to export development certificate");
        }

        if (Enum.TryParse<StoreLocation>(StoreLocation, out var storeLocation))
        {
            using var store = new X509Store(StoreName, storeLocation, OpenFlags.ReadOnly);
#if NET9_0_OR_GREATER
            using var cert = X509CertificateLoader.LoadCertificateFromFile(certPath);
#else
            using var cert = X509Certificate2.CreateFromPem(File.ReadAllText(certPath));
#endif
            if (cert is null)
            {
                throw new InvalidOperationException($"Failed to load certificate from {certPath}");
            }

            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, true);
            if (certs.Count == 0)
            {
                throw new InvalidOperationException($"Valid certificate with thumbprint {cert.Thumbprint} not found in store {StoreName}");
            }
            return certs[0].Thumbprint;
        }
        else
        {
            throw new ArgumentException($"Invalid store location: {StoreLocation}");
        }
    }
}