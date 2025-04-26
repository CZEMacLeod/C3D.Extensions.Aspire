namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class ClrVersionArgumentAnnotation(string clrVersion) : IISExpressArgumentAnnotation
{
    public ClrVersionArgumentAnnotation(Version version) : this($"v{version.Major}.{version.Minor}") { }

    public const string DefaultClrVersion = "v4.0";

    public string ClrVersion => clrVersion;

    public override string ToString() => $"/clr:{clrVersion}";
}
