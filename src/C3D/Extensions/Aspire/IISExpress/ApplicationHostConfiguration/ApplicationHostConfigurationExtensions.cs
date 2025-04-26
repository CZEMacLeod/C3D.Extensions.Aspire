using Aspire.Hosting;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using System.Xml.Serialization;

namespace C3D.Extensions.Aspire.IISExpress;

internal static class ApplicationHostConfigurationExtensions
{
    private const string applicationhostfileName = "applicationhost.config";

    public static ApplicationHostConfiguration GetDefaultConfiguration(this IISExpressResource iis) =>
        iis.Bitness.GetDefaultConfiguration();

    public static ApplicationHostConfiguration GetDefaultConfiguration(this IISExpressProjectResource project) =>
        project.Bitness.GetDefaultConfiguration();

    public static ApplicationHostConfiguration GetDefaultConfiguration(this IISExpressBitness bitness)
    {
        var configPath = Path.Combine(bitness.GetIISExpressPath(), "config", "templates", "PersonalWebServer", applicationhostfileName);
        return Load(configPath) ?? throw new InvalidOperationException("Could not load default applicationhost.config for IIS Express");
    }

    public static ApplicationHostConfiguration? LoadConfiguration(this ConfigArgumentAnnotation annotation) => 
        Load(annotation.ApplicationHostConfig);

    public static void SaveConfiguration(this ConfigArgumentAnnotation annotation, ApplicationHostConfiguration config)
    {
        config.Save(annotation.ApplicationHostConfig);
    }

    public static string SaveConfiguration(this IISExpressResource iis, ApplicationHostConfiguration config)
    {
        var temp = iis.GetConfigurationPath();
        config.Save(temp);

        return temp;
    }

    public static ConfigArgumentAnnotation WithTemporaryConfiguration(this IISExpressProjectResource resource, ApplicationHostConfiguration config)
    {
        var annotation = new ConfigArgumentAnnotation(GetTempConfigFile());
        annotation.SaveConfiguration(config);
        var old = resource.Annotations.OfType<ConfigArgumentAnnotation>().ToList();
        foreach(var a in old)
        {
            resource.Annotations.Remove(a);
        }
        resource.Annotations.Add(annotation);
        return annotation;
    }

    public static string GetConfigurationPath(this IISExpressResource iis) => Path.Combine(iis.TempDirectory, applicationhostfileName);

    private static ApplicationHostConfiguration? Load(string configPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(configPath, nameof(configPath));
        var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));
        if (!File.Exists(configPath))
        {
            return null;
        }
        using var fs = File.OpenRead(configPath);
        return serializer.Deserialize(fs) as ApplicationHostConfiguration;
    }

    public static void Save(this ApplicationHostConfiguration config, Stream stream)
    {
        var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));
        serializer.Serialize(stream, config);
    }

    public static void Save(this ApplicationHostConfiguration config, string fileName)
    {
        using var fs = File.OpenWrite(fileName);
        config.Save(fs);
    }

    internal static string GetTempConfigDir() =>
        System.IO.Directory.CreateTempSubdirectory("aspire.iisexpress.").FullName;

    internal static string GetTempConfigFile() => Path.Combine(GetTempConfigDir(), applicationhostfileName);

    public static Site? GetSite(this ApplicationHostConfiguration appHostConfig, string siteName) =>
        appHostConfig.SystemApplicationHost.Sites.Site
            .SingleOrDefault(s => string.Equals(s.Name, siteName, StringComparison.OrdinalIgnoreCase));
}
