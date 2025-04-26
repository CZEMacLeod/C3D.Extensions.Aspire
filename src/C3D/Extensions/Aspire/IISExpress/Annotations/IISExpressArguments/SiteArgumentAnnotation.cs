namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class SiteArgumentAnnotation : IISExpressArgumentAnnotation
{
    private readonly string site;

    public string Site => site;

    public SiteArgumentAnnotation(string site) => this.site = site;

    public override string ToString() => $"/site:{site}";
}
