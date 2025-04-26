namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class ConfigArgumentAnnotation : IISExpressArgumentAnnotation
{
    private readonly string applicationHostConfig;

    public ConfigArgumentAnnotation(string applicationHostConfig) => this.applicationHostConfig = applicationHostConfig;

    public string ApplicationHostConfig => applicationHostConfig;

    public override string ToString() => $"/config:{applicationHostConfig}";
}