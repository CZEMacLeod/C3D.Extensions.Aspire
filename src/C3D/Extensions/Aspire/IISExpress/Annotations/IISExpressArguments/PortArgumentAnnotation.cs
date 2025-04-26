namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class PortArgumentAnnotation : IISExpressArgumentAnnotation
{
    private readonly int port;

    public PortArgumentAnnotation(int port)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 0, nameof(port));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, nameof(port));
        this.port = port;
    }

    public int Port => port;

    public const int DefaultPort = 8080;

    public override string ToString() => $"/port:{port}";
}
