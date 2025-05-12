using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using System.Diagnostics;

namespace C3D.Extensions.Aspire.Fluent.Annotations;

public class UnderTestAnnotation : IResourceAnnotation
{
    public UnderTestAnnotation() => IsUnderTest = new StackTrace().ContainsAspireTesting();

    public bool IsUnderTest { get; set; }
}
