namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class SiteIdArgumentAnnotation(string siteId) : IISExpressArgumentAnnotation
{
    public string TraceLevel => siteId;

    public override string ToString() => $"/siteid:{siteId}";
}
