using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class CertificateFileAnnotation : CertificateAnnotationBase
{
    public string? CertificateHash { get; protected set; }
    public string CertificateFileName { get; protected set; }
    public string CertificateKeyFileName { get; protected set; }

    public string? FriendlyName { get; set; }

    public CertificateFileAnnotation(string certFileName, string certKeyFileName, string storeName = "My") : base(storeName, "LocalMachine")
    {
        CertificateFileName = certFileName;
        CertificateKeyFileName = certKeyFileName;
    }

    public override string ToString() => CertificateHash ?? string.Empty;

    public override async Task<string> GetCertificateThumbprintAsync(IServiceProvider? serviceProvider)
    {
        var logger = serviceProvider?.GetRequiredService<ILogger<CertificateHashAnnotation>>() ?? NullLogger<CertificateHashAnnotation>.Instance;

        if (string.IsNullOrEmpty(CertificateFileName) || string.IsNullOrEmpty(CertificateKeyFileName))
        {
            throw new ArgumentException("Certificate file name or key file name is not set.");
        }
        if (!System.IO.File.Exists(CertificateFileName) || !System.IO.File.Exists(CertificateKeyFileName))
        {
            throw new ArgumentException("Certificate file name or key file name does not exist.");
        }

        using var cert = X509Certificate2.CreateFromPem(File.ReadAllText(CertificateFileName), File.ReadAllText(CertificateKeyFileName));
        if (cert is null)
        {
            throw new InvalidOperationException($"Failed to load certificate from {CertificateFileName} / {CertificateKeyFileName}");
        }
        if (!cert.Verify())
        {
            throw new InvalidOperationException($"Certificate {cert.Thumbprint} is not valid");
        }
        cert.FriendlyName = FriendlyName ?? cert.FriendlyName ?? cert.Subject;

        if (!VerifyStore(logger, cert))
        {
            if (serviceProvider is null)
            {
                throw new InvalidOperationException("Service provider is null and certificate store verification failed.");
            }
            var cmd = serviceProvider.GetRequiredService<CommandExecutor>();

            var tmp = Path.ChangeExtension(Path.GetTempFileName(), "pfx");
            try
            {
                var pwd = Guid.NewGuid().ToString("N");
                await File.WriteAllBytesAsync(tmp, cert.Export(X509ContentType.Pfx, pwd));

                logger.LogInformation("Adding certificate to store (Requires Elevation)");
                cmd.CensoredArgs.Add(pwd);  // Prevent showing password in output
                string[] args = ["-p", pwd, "-importPFX", "-f", StoreName, tmp];
                if (!string.IsNullOrEmpty(FriendlyName))
                {
                    args = args.Append($"FriendlyName={FriendlyName}").ToArray();
                }
                var exitCode = await cmd.ExecuteAdminCommandAsync("certutil", args);
            }
            finally
            {
                try
                {
                    File.Delete(tmp);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete temporary file {TempFile}", tmp);
                }
            }

            if (!VerifyStore(logger, cert))
            {
                throw new InvalidOperationException($"Failed to add certificate {cert.Thumbprint} to store {StoreName}");
            }
        }

        CertificateHash = cert.Thumbprint;
        return CertificateHash;
    }

    private bool VerifyStore(ILogger<CertificateHashAnnotation> logger, X509Certificate2 cert)
    {
        using var store = new X509Store(StoreName, storeLocation, OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, true);
        if (certs.Count == 0 || !certs.Any(c => c.HasPrivateKey))
        {
            logger.LogInformation("Certificate {Certificate} {Name} does not exist in certificate store {StoreLocation}/{StoreName}", cert.Thumbprint, cert.FriendlyName ?? cert.Subject, StoreLocation, StoreName);
            store.Close();
            store.Open(OpenFlags.MaxAllowed);   // With MaxAllowed, we seem to fail silently when trying to add the certificate.
            try
            {
                store.Certificates.Add(cert);
                certs = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, true);
                if (certs.Count == 1 && certs[0].HasPrivateKey)
                {
                    logger.LogInformation("Added certificate {Thumbprint} {Name} to {StoreLocation}/{StoreName}", cert.Thumbprint, cert.FriendlyName ?? cert.Subject, StoreLocation, StoreName);
                }
                else
                {
                    return false;
                }
            }
            catch (CryptographicException ex)
            {
                logger.LogWarning(ex, "Failed to add certificate {Thumbprint} {Name} to {StoreLocation}/{StoreName}", cert.Thumbprint, cert.FriendlyName ?? cert.Subject, StoreLocation, StoreName);
                return false;
            }
        }
        return true;
    }
}