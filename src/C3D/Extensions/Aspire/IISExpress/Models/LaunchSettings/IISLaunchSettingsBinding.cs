using System.Text.Json.Serialization;

namespace C3D.Extensions.Aspire.IISExpress.Models.LaunchSettings
{
    public class IISLaunchSettingsBinding
    {
        [JsonPropertyName("applicationUrl")]
        public string? ApplicationUrl { get; set; }

        [JsonPropertyName("sslPort")]
        public int? SSLPort { get; set; }
    }
}