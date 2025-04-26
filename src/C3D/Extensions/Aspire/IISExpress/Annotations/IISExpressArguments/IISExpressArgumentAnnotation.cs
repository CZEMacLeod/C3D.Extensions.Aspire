using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public abstract class IISExpressArgumentAnnotation : IResourceAnnotation, IValueProvider
{
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ToString());
}