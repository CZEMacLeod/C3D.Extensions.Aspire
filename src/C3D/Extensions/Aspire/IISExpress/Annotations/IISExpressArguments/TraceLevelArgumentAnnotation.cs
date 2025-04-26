namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class TraceLevelArgumentAnnotation(IISExpressTraceLevel traceLevel) : IISExpressArgumentAnnotation
{
    public IISExpressTraceLevel TraceLevel => traceLevel;
    public const IISExpressTraceLevel DefaultTraceLevel = IISExpressTraceLevel.None;

    public override string ToString() => $"/trace:{traceLevel.ToString().ToLowerInvariant()}";
}
