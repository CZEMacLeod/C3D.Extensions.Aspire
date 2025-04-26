namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class PathArgumentAnnotation(string sitePath) : IISExpressArgumentAnnotation
{
    public string SitePath => sitePath;

    public override string ToString() => $"/path:{sitePath}";
}