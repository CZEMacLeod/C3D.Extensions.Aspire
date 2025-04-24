using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

public static class ApplicationHostConfigurationExtensions
{
    public static ApplicationHostConfiguration GetDefaultConfiguration(this IISExpressResource iis)
    {
        var configPath = Path.Combine(iis.Directory, "config", "templates", "PersonalWebServer", "applicationhost.config");

        var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));

        using var fs = File.OpenRead(configPath);
        return serializer.Deserialize(fs) as ApplicationHostConfiguration ?? throw new InvalidOperationException("Could not load default applicationhost.config for IIS Express");
    }

    public static void Save(this ApplicationHostConfiguration config, Stream stream)
    {
        var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));

        serializer.Serialize(stream, config);
    }

    public static string SaveConfiguration(this IISExpressResource iis, ApplicationHostConfiguration config)
    {
        var temp = iis.GetConfigurationPath();

        using var fs = File.OpenWrite(temp);
        config.Save(fs);

        return temp;
    }

    public static string GetConfigurationPath(this IISExpressResource iis)
    {
        return Path.Combine(iis.TempDirectory, "applicationhost.config");
    }
}
