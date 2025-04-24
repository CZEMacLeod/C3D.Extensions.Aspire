using System.Buffers.Text;
using System.Text.Json.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

internal class NetshSslCert
{
    public required SslCertificateBinding[] SslCertificateBindings { get; init; }
    [JsonPropertyName("message")]
    public required string Message { get; init; }
    [JsonPropertyName("status")] 
    public int Status { get; init; }
}

public class SslCertificateBinding
{
    public required string IpPort { get; init; }
    public int ConfigType { get; init; }
    public Guid AppId { get; init; }

    public required byte[] SslHash { get; init; }
    public string Thumbprint => Convert.ToHexString(SslHash);

    public required string SslCertStoreName { get; init; }
}