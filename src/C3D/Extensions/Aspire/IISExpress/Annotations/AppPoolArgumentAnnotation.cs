namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class AppPoolArgumentAnnotation : IISExpressArgumentAnnotation
{
    public const string DefaultAppPool = "Clr4IntegratedAppPool";

    public AppPoolArgumentAnnotation(string appPool = DefaultAppPool) => AppPool = appPool;

    public string AppPool { get; }

    public override string ToString() => $"/apppool:{AppPool}";
}
