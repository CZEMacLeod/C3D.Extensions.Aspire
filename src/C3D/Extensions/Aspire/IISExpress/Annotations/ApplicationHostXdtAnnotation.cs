using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class ApplicationHostXdtAnnotation(string filePath, int order = 0) : IResourceAnnotation
{
    public string FilePath { get; } = filePath;
    public int Order { get; } = order;
}
