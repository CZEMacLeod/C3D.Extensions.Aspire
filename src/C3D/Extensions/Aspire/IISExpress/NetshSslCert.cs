namespace C3D.Extensions.Aspire.IISExpress;

internal class NetshSslCert
{
    public required SslCertificateBinding[] SslCertificateBindings { get; init; }
    public required string Message { get; init; }
    public int Status { get; init; }
}

public class SslCertificateBinding
{
    public required string IpPort { get; init; }
    public int ConfigType { get; init; }
    public Guid AppId { get; init; }
    public required string SslHash { get; init; }
    public required string SslCertStoreName { get; init; }
}