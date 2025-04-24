using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class IISCertificateAnnotation : CertificateFileAnnotation
{
    public IISCertificateAnnotation() : base(null!, null!, "My")
    {
    }

    public override async Task<string> GetCertificateThumbprintAsync(IServiceProvider? serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

        var logger = serviceProvider.GetRequiredService<ILogger<CertificateHashAnnotation>>() ?? NullLogger<CertificateHashAnnotation>.Instance;
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var (exported, certPath, certKeyPath) = await IISExpressCertificateHostingExtensions.TryExportIISCertificateAsync(serviceProvider, logger);
        if (!exported)
        {
            throw new InvalidOperationException("Failed to export iis certificate");
        }

        this.CertificateFileName = certPath;
        this.CertificateKeyFileName = certKeyPath;

        return await base.GetCertificateThumbprintAsync(serviceProvider);
    }
}
