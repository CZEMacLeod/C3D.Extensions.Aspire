using System.Text.Json.Serialization;

namespace C3D.Extensions.Aspire.IISExpress.Models.LaunchSettings;

public class IISSettings
{
    [JsonPropertyName("windowsAuthentication")]
    public bool WindowsAuthentication { get; set; } = false;

    [JsonPropertyName("anonymousAuthentication")]
    public bool AnonymousAuthentication { get; set; } = true;


    [JsonPropertyName("iis")]
    public IISLaunchSettingsBinding? IIS { get; set; }

    [JsonPropertyName("iisExpress")] 
    public IISLaunchSettingsBinding? IISExpress { get; set; }
}