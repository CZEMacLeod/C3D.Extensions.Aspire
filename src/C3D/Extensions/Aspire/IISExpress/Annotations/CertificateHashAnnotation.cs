using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography.X509Certificates;
namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class CertificateHashAnnotation : CertificateAnnotationBase
{
    public CertificateHashAnnotation(string certificateHash, string storeName = "My", string storeLocation = "LocalMachine") : base(storeName, storeLocation)
    {
        CertificateHash = certificateHash;

        using var store = new X509Store(StoreName, base.storeLocation, OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, CertificateHash, true);
        if (certs.Count == 0)
        {
            throw new ArgumentException($"Valid certificate with thumbprint {CertificateHash} not found in store {StoreLocation}/{StoreName}", nameof(certificateHash));
        }
    }

    public string CertificateHash { get; }

    public override Task<string> GetCertificateThumbprintAsync(IServiceProvider? serviceProvider) => Task.FromResult(CertificateHash);
}
