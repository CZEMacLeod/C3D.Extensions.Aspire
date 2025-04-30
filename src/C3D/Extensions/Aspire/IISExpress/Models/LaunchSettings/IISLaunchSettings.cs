using Aspire.Hosting;
using System.Text.Json.Serialization;

namespace C3D.Extensions.Aspire.IISExpress.Models.LaunchSettings;

public class IISLaunchSettings // : LaunchSettings  // This is sealed and does not include IISSettings
{
    [JsonPropertyName("iisSettings")]
    public IISSettings IISSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of named launch profiles associated with the <see cref="ApplicationModel.ProjectResource"/>.
    /// </summary>
    [JsonPropertyName("profiles")]
    public Dictionary<string, LaunchProfile> Profiles { get; set; } = [];
}