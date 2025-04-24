using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace C3D.Extensions.Aspire.IISExpress;

public static class IISExpressCertificateHostingExtensions
{
    /// <summary>
    /// Injects the IIS Express Development Certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// If the resource is a <see cref="ContainerResource"/>, the certificate files will be bind mounted into the container.
    /// </summary>
    /// <remarks>
    /// This method <strong>does not</strong> configure an HTTPS endpoint on the resource.
    /// Use <see cref="ResourceBuilderExtensions.WithHttpsEndpoint{TResource}"/> to configure an HTTPS endpoint.
    /// </remarks>
    public static IResourceBuilder<TResource> RunWithIISExpressDeveloperCertificate<TResource>(
        this IResourceBuilder<TResource> builder, string certFileEnv, string certKeyFileEnv, Action<string, string>? onSuccessfulExport = null)
        where TResource : IResourceWithEnvironment
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>(async (e, ct) =>
            {
                var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(builder.Resource);

                // Export the ASP.NET Core HTTPS development certificate & private key to files and configure the resource to use them via
                // the specified environment variables.
                var (exported, certPath, certKeyPath) = await TryExportIISCertificateAsync(e.Services, logger);

                if (!exported)
                {
                    // The export failed for some reason, don't configure the resource to use the certificate.
                    return;
                }

                if (builder.Resource is ContainerResource containerResource)
                {
                    // Bind-mount the certificate files into the container.
                    const string DEV_CERT_BIND_MOUNT_DEST_DIR = "/dev-certs";

                    var certFileName = Path.GetFileName(certPath);
                    var certKeyFileName = Path.GetFileName(certKeyPath);

                    var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

                    var certFileDest = $"{DEV_CERT_BIND_MOUNT_DEST_DIR}/{certFileName}";
                    var certKeyFileDest = $"{DEV_CERT_BIND_MOUNT_DEST_DIR}/{certKeyFileName}";

                    builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                        .WithBindMount(bindSource, DEV_CERT_BIND_MOUNT_DEST_DIR, isReadOnly: true)
                        .WithEnvironment(certFileEnv, certFileDest)
                        .WithEnvironment(certKeyFileEnv, certKeyFileDest);
                }
                else
                {
                    builder
                        .WithEnvironment(certFileEnv, certPath)
                        .WithEnvironment(certKeyFileEnv, certKeyPath);
                }

                if (onSuccessfulExport is not null)
                {
                    onSuccessfulExport(certPath, certKeyPath);
                }
            });
        }

        return builder;
    }

    internal static async Task<(bool, string CertFilePath, string CertKeyFilPath)> TryExportIISCertificateAsync(IServiceProvider serviceProvider, ILogger logger)
    {

        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Exports the ASP.NET Core HTTPS development certificate & private key to PEM files using 'dotnet dev-certs https' to a temporary
        // directory and returns the path.
        // TODO: Check if we're running on a platform that already has the cert and key exported to a file (e.g. macOS) and just use those instead.
        var appNameHash = configuration["AppHost:Sha256"]![..10];
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire.{appNameHash}");
        var certExportPath = Path.Combine(tempDir, "iis-cert.pem");
        var certKeyExportPath = Path.Combine(tempDir, "iis-cert.key");

        if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            // Certificate already exported, return the path.
            logger.LogDebug("Using previously exported dev cert files '{CertPath}' and '{CertKeyPath}'", certExportPath, certKeyExportPath);
            return (true, certExportPath, certKeyExportPath);
        }

        if (File.Exists(certExportPath))
        {
            logger.LogTrace("Deleting previously exported dev cert file '{CertPath}'", certExportPath);
            File.Delete(certExportPath);
        }

        if (File.Exists(certKeyExportPath))
        {
            logger.LogTrace("Deleting previously exported dev cert key file '{CertKeyPath}'", certKeyExportPath);
            File.Delete(certKeyExportPath);
        }

        if (!Directory.Exists(tempDir))
        {
            logger.LogTrace("Creating directory to export dev cert to '{ExportDir}'", tempDir);
            Directory.CreateDirectory(tempDir);
        }

        const string friendlyName = "IIS Express Development Certificate";

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
        var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "localhost", false)
                .Where(c => c.HasPrivateKey && c.FriendlyName == friendlyName);
        var cert = certs.FirstOrDefault();
        if (cert is null)
        {
            logger.LogError("Valid certificate with friendly name '{FriendlyName}' not found in store '{StoreLocation}'/'{StoreName}'", friendlyName, store.Location, store.Name);
            return (false, certExportPath, certKeyExportPath);
        }
        logger.LogDebug("Exporting certificate '{Thumbprint}' to '{CertPath}' and '{CertKeyPath}'", cert.Thumbprint, certExportPath, certKeyExportPath);
        try
        {
            return await ExportCertificate(logger, certExportPath, certKeyExportPath, cert);
        }
        catch (CryptographicException ex)
        {
            logger.LogWarning(ex, "Failed to export certificate '{Thumbprint}' to '{CertPath}' and '{CertKeyPath}'", cert.Thumbprint, certExportPath, certKeyExportPath);
            var cmd = serviceProvider.GetRequiredService<CommandExecutor>();
            var tmp = Path.ChangeExtension(Path.GetTempFileName(), ".pfx");
            File.Delete(tmp);
            try
            {
                var pwd = Guid.NewGuid().ToString("N");
                cmd.CensoredArgs.Add(pwd);
                await cmd.ExecuteAdminCommandAsync("certutil.exe", "-exportPFX", "-p", pwd, store.Name!.ToString(), tmp, "ExtendedProperties");
#if NET9_0_OR_GREATER
                var cert2 = X509CertificateLoader.LoadPkcs12FromFile(tmp, pwd, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#else
                var cert2 = new X509Certificate2(tmp, pwd, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#endif
                return await ExportCertificate(logger, certExportPath, certKeyExportPath, cert2);
            }
            finally
            {
                if (File.Exists(tmp))
                {
                    File.Delete(tmp);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export certificate '{Thumbprint}' to '{CertPath}' and '{CertKeyPath}'", cert.Thumbprint, certExportPath, certKeyExportPath);
            return (false, certExportPath, certKeyExportPath);
        }
    }

    private static async Task<(bool, string CertFilePath, string CertKeyFilPath)> ExportCertificate(ILogger logger, string certExportPath, string certKeyExportPath, X509Certificate2 cert)
    {
        // Export the certificate to a file.
        var certPem = cert.ExportCertificatePem();
        await File.WriteAllTextAsync(certExportPath, certPem);
        logger.LogDebug("Exported certificate '{Thumbprint}' to '{CertPath}'", cert.Thumbprint, certExportPath);
        // Export the private key to a file.
        var key = cert.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem();
        await File.WriteAllTextAsync(certKeyExportPath, key);
        logger.LogDebug("Exported private key '{Thumbprint}' to '{CertKeyPath}'", cert.Thumbprint, certKeyExportPath);
        return (true, certExportPath, certKeyExportPath);
    }
}
