using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.Node.Annotations;

public class NodeOptionAnnotation : IResourceAnnotation, IValueProvider, IManifestExpressionProvider
{
    private readonly IValueProvider? valueProvider;
    private readonly IManifestExpressionProvider? manifestExpressionProvider;

    private readonly string? value;

    public static NodeOptionAnnotation Create(string value) => new(value);

    public static NodeOptionAnnotation Create<T>(T value)
        where T : IValueProvider, IManifestExpressionProvider => new(value, value);

    private NodeOptionAnnotation(IValueProvider valueProvider, IManifestExpressionProvider manifestExpressionProvider)
    {
        this.valueProvider = valueProvider;
        this.manifestExpressionProvider = manifestExpressionProvider;
    }

    public NodeOptionAnnotation(string value) => this.value = value;

    public string ValueExpression => manifestExpressionProvider?.ValueExpression ?? value!;

    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) =>
        valueProvider?.GetValueAsync(cancellationToken) ?? ValueTask.FromResult(value);
}
