using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress;
using C3D.Extensions.Aspire.IISExpress.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Xml.Linq;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class LaunchDetailsExtensions
{
    private static readonly Guid WebProjectGuid = new("{349c5851-65df-11da-9384-00065b846f21}");

    public static IResourceBuilder<T> WithIISLaunchDetails<T>(this IResourceBuilder<T> resource, IISProjectLaunchDetailsAnnotation metadata)
        where T : IResourceWithEndpoints, IResourceWithEnvironment
    {
        resource.WithAnnotation(new IISExpressBitnessAnnotation(metadata.Bitness));

        resource.WithAnnotation(metadata);

        if (metadata is { HttpPort: { } http })
        {
            resource.WithHttpEndpoint(targetPort: http);
        }

        if (metadata is { SslPort: { } https })
        {
            resource.WithHttpsEndpoint(targetPort: https);
        }

        resource.WithEnvironment(ctx =>
        {
            foreach (var envVar in metadata.EnvironmentVariables)
            {
                ctx.EnvironmentVariables[envVar.Key] = envVar.Value;
            }
        });

        return resource;
    }

    internal static IISProjectLaunchDetailsAnnotation GetLaunchDetails(this IProjectMetadata metadata)
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

    internal record class IISProjectLaunchDetailsAnnotation : IResourceAnnotation
    {
        public bool Use64BitIISExpress { get; init; } = true;

        public IISExpressBitness Bitness => Use64BitIISExpress ? IISExpressBitness.IISExpress64Bit : IISExpressBitness.IISExpress32Bit;

        public int? SslPort { get; init; }

        public int? HttpPort { get; init; }

        public ICollection<KeyValuePair<string, string>> EnvironmentVariables { get; init; } = [];
    }

    private static IISProjectLaunchDetailsAnnotation ParseLaunchSettingsJson(string path)
    {
        using var stream = File.OpenRead(path);
        var metadata = new IISProjectLaunchDetailsAnnotation();

        if (System.Text.Json.JsonSerializer.Deserialize<IISLaunchSettings>(stream) is { Settings.IISExpress: { } iisExpressSettings } settings)
        {
            if (settings.GetIISExpressProfile() is { } profile)
            {
                metadata = metadata with
                {
                    Use64BitIISExpress = profile.Use64Bit,
                    EnvironmentVariables = profile.EnvironmentVariables
                };
            }

            metadata = metadata with
            {
                SslPort = iisExpressSettings.SslPort
            };

            if (iisExpressSettings.ApplicationUrl is { })
            {
                var url = new Uri(iisExpressSettings.ApplicationUrl);

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
        public IEnumerable<KeyValuePair<string, IISLaunchProfile>> Profiles { get; set; } = [];

        public IISLaunchProfile? GetIISExpressProfile()
        {
            foreach (var profile in Profiles)
            {
                if (profile.Value.CommandName is { } cmd && cmd.Equals("IISExpress", StringComparison.OrdinalIgnoreCase))
                {
                    return profile.Value;
                }
            }

            return null;
        }

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

        public class IISLaunchProfile
        {  /// <summary>
           /// Gets or sets the name of the launch profile.
           /// </summary>
            [JsonPropertyName("commandName")]
            public string? CommandName { get; set; }

            /// <summary>
            /// Gets or sets the command line arguments for the launch profile.
            /// </summary>
            [JsonPropertyName("commandLineArgs")]
            public string? CommandLineArgs { get; set; }

            /// <summary>
            /// Gets or sets whether the project is configured to emit logs when running with dotnet run.
            /// </summary>
            [JsonPropertyName("dotnetRunMessages")]
            public bool? DotnetRunMessages { get; set; }

            /// <summary>
            /// Gets or sets the launch browser flag for the launch profile.
            /// </summary>
            [JsonPropertyName("launchBrowser")]
            public bool? LaunchBrowser { get; set; }

            /// <summary>
            /// Gets or sets the launch URL for the launch profile.
            /// </summary>
            [JsonPropertyName("launchUrl")]
            public string? LaunchUrl { get; set; }

            /// <summary>
            /// Gets or sets the application URL for the launch profile.
            /// </summary>
            [JsonPropertyName("applicationUrl")]
            public string? ApplicationUrl { get; set; }

            /// <summary>
            /// Gets or sets the environment variables for the launch profile.
            /// </summary>
            [JsonPropertyName("environmentVariables")]
            public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

            [JsonPropertyName("use64Bit")]
            public bool Use64Bit { get; set; }
        }
    }

    private static bool TryParseOldStyleProject(string path, [MaybeNullWhen(false)] out IISProjectLaunchDetailsAnnotation metadata)
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

        metadata = new IISProjectLaunchDetailsAnnotation
        {
            Use64BitIISExpress = use64bitIISExpress,
            SslPort = sslPort,
            HttpPort = port,
        };
        return true;
    }
}
