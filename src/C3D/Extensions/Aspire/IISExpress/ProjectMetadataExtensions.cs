using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Xml.Linq;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class ProjectMetadataExtensions
{
    private static readonly Guid WebProjectGuid = new("{349c5851-65df-11da-9384-00065b846f21}");

    public static IResourceBuilder<T> RegisterProjectDetails<T>(this IResourceBuilder<T> resource, IProjectMetadata projectMetadata)
        where T : IResourceWithEndpoints
    {
        var metadata = projectMetadata.GetMetadata();

        resource.WithAnnotation(projectMetadata);
        resource.WithAnnotation(metadata);

        if (metadata is { HttpPort: { } http })
        {
            resource.WithHttpEndpoint(targetPort: http);
        }

        if (metadata is { SslPort: { } https })
        {
            resource.WithHttpsEndpoint(targetPort: https);
        }

        return resource;
    }

    private static IISProjectMetadataAnnotation GetMetadata(this IProjectMetadata metadata)
    {
        var launchJsonPath = Path.Combine(Path.GetDirectoryName(metadata.ProjectPath)!, "Properties", "launchSettings.json");

        if (File.Exists(launchJsonPath))
        {
            return ParseLaunchSettingsJson(launchJsonPath);
        }
        else if (TryParseOldStyleProject(metadata.ProjectPath, out var oldStyleMetadata))
        {
            return oldStyleMetadata;
        }

        return new();
    }

    internal record class IISProjectMetadataAnnotation : IResourceAnnotation
    {
        public bool Use64BitIISExpress { get; init; } = true;

        public int? SslPort { get; init; }

        public int? HttpPort { get; init; }
    }

    private static IISProjectMetadataAnnotation ParseLaunchSettingsJson(string path)
    {
        using var stream = File.OpenRead(path);
        var metadata = new IISProjectMetadataAnnotation();

        if (System.Text.Json.JsonSerializer.Deserialize<IISLaunchSettings>(stream) is { Settings.IISExpress: { } settings })
        {
            metadata = metadata with
            {
                SslPort = settings.SslPort
            };

            if (settings.ApplicationUrl is { })
            {
                var url = new Uri(settings.ApplicationUrl);

                metadata = metadata with
                {
                    HttpPort = url.Port,
                };
            }
        }

        return metadata;

    }


    public class IISLaunchSettings
    {
        [JsonPropertyName("iisSettings")]
        public IISSettings? Settings { get; set; }

        public class IISSettings
        {
            [JsonPropertyName("iisExpress")]
            public IISExpressDetails? IISExpress { get; set; }
        }

        public class IISExpressDetails
        {
            [JsonPropertyName("applicationUrl")]
            public string? ApplicationUrl { get; set; }

            [JsonPropertyName("sslPort")]
            public int? SslPort { get; set; }
        }
    }

    private static bool TryParseOldStyleProject(string path, [MaybeNullWhen(false)] out IISProjectMetadataAnnotation metadata)
    {
        XNamespace MsbuildNS = "http://schemas.microsoft.com/developer/msbuild/2003";

        var doc = XDocument.Load(path);

        var project = doc.Descendants(MsbuildNS + "Project").SingleOrDefault();

        if (project is not { })
        {
            metadata = null;
            return false;
        }

        var propertyGroups = project
            .Descendants(MsbuildNS + "PropertyGroup");
        var use64bitIISExpress = propertyGroups
            .Descendants(MsbuildNS + "Use64BitIISExpress")
            .FirstOrDefault() is { } b && bool.TryParse(b.Value, out var use64BitValue) ? use64BitValue : true;
        var sslPort = propertyGroups
            .Descendants(MsbuildNS + "IISExpressSSLPort")
            .FirstOrDefault() is { } s && int.TryParse(s.Value, out var sslPortValue) ? sslPortValue : default;
        var webProjectProperties = project
            .Descendants(MsbuildNS + "ProjectExtensions")
            .Descendants(MsbuildNS + "VisualStudio")
            .Descendants(MsbuildNS + "FlavorProperties")
            .FirstOrDefault(flavor => flavor.Attribute("GUID") is { } g && Guid.TryParse(g.Value, out var guid) && guid == WebProjectGuid);
        var port = webProjectProperties?
            .Descendants(MsbuildNS + "DevelopmentServerPort")
            .FirstOrDefault() is { } p && int.TryParse(p.Value, out var portValue) ? portValue : default;

        metadata = new IISProjectMetadataAnnotation
        {
            Use64BitIISExpress = use64bitIISExpress,
            SslPort = sslPort,
            HttpPort = port,
        };
        return true;
    }
}
