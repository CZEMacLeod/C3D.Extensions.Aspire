namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class AppPoolArgumentAnnotation : IISExpressArgumentAnnotation
{
    private readonly string appPool;
    public const string DefaultAppPool = "Clr4IntegratedAppPool";

    public AppPoolArgumentAnnotation(string appPool = DefaultAppPool) => this.appPool = appPool;

    public override string ToString() => $"/apppool:{appPool}";
}
