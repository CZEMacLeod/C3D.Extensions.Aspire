using C3D.Extensions.Aspire.IISExpress.Annotations;
using C3D.Extensions.Aspire.IISExpress.Configuration;
using C3D.Extensions.Aspire.IISExpress.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.XmlTransform;
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

    public static void SaveConfiguration(this ConfigArgumentAnnotation annotation, ApplicationHostConfiguration config, ILogger? logger = null, params IEnumerable<ApplicationHostXdtAnnotation> xdts)
    {
        config.Save(annotation.ApplicationHostConfig, logger, xdts);
    }

    public static string SaveConfiguration(this IISExpressResource iis, ApplicationHostConfiguration config, ILogger? logger = null)
    {
        var temp = iis.GetConfigurationPath();
        config.Save(temp, logger, iis.Annotations.OfType<ApplicationHostXdtAnnotation>());

        return temp;
    }

    public static ConfigArgumentAnnotation WithTemporaryConfiguration(this IISExpressProjectResource resource, ApplicationHostConfiguration config, ILogger? logger = null, params IEnumerable<ApplicationHostXdtAnnotation> xdts)
    {
        var annotation = new ConfigArgumentAnnotation(GetTempConfigFile());
        annotation.SaveConfiguration(config, logger, xdts);
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

    private class XdtLoggerAdapter(ILogger logger) : IXmlTransformationLogger, IDisposable
    {
        private readonly List<IDisposable> scopes = new List<IDisposable>();
        private bool disposedValue;

        public void EndSection(string message, params object[] messageArgs) => EndSection(MessageType.Normal, message, messageArgs);

        public void EndSection(MessageType type, string message, params object[] messageArgs)
        {
            logger.Log(type switch { MessageType.Normal => LogLevel.Information, MessageType.Verbose => LogLevel.Trace, _ => LogLevel.Information }, message, messageArgs);
            var scope = scopes.LastOrDefault();
            if (scope is not null)
            {
                scope.Dispose();
                scopes.Remove(scope);
            }
        }

        public void LogError(string message, params object[] messageArgs) => logger.LogError(message, messageArgs);

        public void LogError(string file, string message, params object[] messageArgs) => logger.LogError(message, messageArgs);

        public void LogError(string file, int lineNumber, int linePosition, string message, params object[] messageArgs) => logger.LogError(message, messageArgs);

        public void LogErrorFromException(Exception ex) => logger.LogError(ex, ex.Message);

        public void LogErrorFromException(Exception ex, string file) => logger.LogError(ex, "An error occurred in {File} : {Message}", file, ex.Message);
        public void LogErrorFromException(Exception ex, string file, int lineNumber, int linePosition)
             => logger.LogError(ex, "An error occurred in {File} @ {LineNumber}/{LinePosition} : {Message}", file, lineNumber, linePosition, ex.Message);

        public void LogMessage(string message, params object[] messageArgs) => LogMessage(MessageType.Normal, message, messageArgs);

        public void LogMessage(MessageType type, string message, params object[] messageArgs) => 
            logger.Log(type switch { MessageType.Normal => LogLevel.Information, MessageType.Verbose => LogLevel.Trace, _ => LogLevel.Information }, message, messageArgs);

        public void LogWarning(string message, params object[] messageArgs) => logger.LogWarning(message, messageArgs);

        public void LogWarning(string file, string message, params object[] messageArgs) => logger.LogWarning(message, messageArgs);

        public void LogWarning(string file, int lineNumber, int linePosition, string message, params object[] messageArgs) => logger.LogWarning(message, messageArgs);

        public void StartSection(string message, params object[] messageArgs) => StartSection(MessageType.Normal, message, messageArgs);

        public void StartSection(MessageType type, string message, params object[] messageArgs) {
            if (type == MessageType.Normal || logger.IsEnabled(LogLevel.Trace))
            {
                var scope = logger.BeginScope(message, messageArgs);
                if (scope is not null)
                {
                    scopes.Add(scope);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    scopes.ForEach(scopes => scopes.Dispose());
                    scopes.Clear();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public static void Save(this ApplicationHostConfiguration config, Stream stream, ILogger? logger = null, params IEnumerable<ApplicationHostXdtAnnotation> xdts)
    {
        if (xdts.Any())
        {
            var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));
            using var memoryStream = new MemoryStream();
            serializer.Serialize(memoryStream, config);
            memoryStream.Position = 0;

            var doc = new XmlTransformableDocument
            {
                PreserveWhitespace = true
            };
            doc.Load(memoryStream);

            using var xdtLogger = new XdtLoggerAdapter(logger ?? NullLogger.Instance);
            foreach (var xdt in xdts.OrderBy(x=>x.Order))
            {
                var transform = new XmlTransformation(xdt.FilePath, true, xdtLogger);
                var success = transform.Apply(doc);
                logger?.LogInformation("Applied {XdtFile} {Success}", xdt.FilePath, success);
            }
            doc.Save(stream);
        } else
        {
            var serializer = new XmlSerializer(typeof(ApplicationHostConfiguration));
            serializer.Serialize(stream, config);
        }
    }

    public static void Save(this ApplicationHostConfiguration config, string fileName, ILogger? logger = null, params IEnumerable<ApplicationHostXdtAnnotation> xdts)
    {
        using var fs = File.OpenWrite(fileName);
        config.Save(fs, logger, xdts);
    }

    internal static string GetTempConfigDir() =>
        System.IO.Directory.CreateTempSubdirectory("aspire.iisexpress.").FullName;

    internal static string GetTempConfigFile() => Path.Combine(GetTempConfigDir(), applicationhostfileName);

    public static Site? GetSite(this ApplicationHostConfiguration appHostConfig, string siteName) =>
        appHostConfig.SystemApplicationHost.Sites.Site
            .SingleOrDefault(s => string.Equals(s.Name, siteName, StringComparison.OrdinalIgnoreCase));
}
