using Aspire.Hosting.ApplicationModel;
using System.Diagnostics;

namespace C3D.Extensions.Aspire.IISExpress.Annotations;

/// <summary>
/// Represents an annotation that specifies the launch profile name for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, LaunchProfileName = {LaunchProfileName}")]
public sealed class IISProfileSettingsAnnotation : IResourceAnnotation
{
    private readonly string iisProfileSettingsName;

    public IISProfileSettingsAnnotation(string iisProfileSettingsName)
    {
        this.iisProfileSettingsName = iisProfileSettingsName ?? throw new ArgumentNullException(nameof(iisProfileSettingsName));
    }

    /// <summary>
    /// Gets the launch profile name.
    /// </summary>
    public string IISProfileSettingsName => iisProfileSettingsName;
}
