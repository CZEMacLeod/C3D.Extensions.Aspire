using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace C3D.Extensions.Aspire.Node;

partial class NodeDebugHook : BackgroundService
{
    private const string connectionStringPropertyName = "debug.v8.connectionString";
    private readonly ILogger<NodeDebugHook> logger;
    private readonly ResourceNotificationService resourceNotificationService;
    private readonly ResourceLoggerService resourceLoggerService;
    private readonly DistributedApplicationModel model;

    public NodeDebugHook(ILogger<NodeDebugHook> logger,
        ResourceNotificationService resourceNotificationService,
        ResourceLoggerService resourceLoggerService,
        DistributedApplicationModel model)
    {
        this.logger = logger;
        this.resourceNotificationService = resourceNotificationService;
        this.resourceLoggerService = resourceLoggerService;
        this.model = model;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = model.GetExecutableResources()
            .OfType<NodeAppResource>()
            .Where(r => r.HasAnnotationOfType<VisualStudioDebug.Annotations.DebugAttachAnnotation>())
            .Select(r => WatchResourceAsync(r, stoppingToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task WatchResourceAsync(NodeAppResource resource, CancellationToken stoppingToken)
    {
        logger.LogInformation("Waiting for debug connection string for {resource}", resource.Name);

        var regex = DetectDebuggerUrl();
        var debugAnnotation = resource.Annotations.OfType<DebugAttachAnnotation>().Last();

        await foreach (var batch in resourceLoggerService.WatchAsync(resource).WithCancellation(stoppingToken))
        {
            foreach (var logLine in batch)
            {
                logger.LogDebug("{resource}: {line}", resource.Name, logLine);

                var match = regex.Match(logLine.Content);
                if (match.Success)
                {
                    var url = match.Groups["url"].Value!;
                    logger.LogInformation("Debugger connection string {url}", url);
                    if (debugAnnotation.DebuggerProcessId is not null)
                    {
                        logger.LogWarning("Previously Debugged");
                        debugAnnotation.DebuggerProcessId = null;
                    }
                    resource.Annotations.Add(new DebugAttachTransportAnnotation()
                    {
                        Transport = VisualStudioDebug.WellKnown.Transports.V8Inspector,
                        Qualifier = url
                    });
                    debugAnnotation.Skip = false;
                    await resourceNotificationService.PublishUpdateAsync(resource, state =>
                    {
                        var old = state.Properties.SingleOrDefault(rps => rps.Name == connectionStringPropertyName);
                        ResourcePropertySnapshot cs = new(connectionStringPropertyName, url);
                        if (old is null)
                        {
                            state.Properties.Add(cs);
                        }
                        else
                        {
                            state.Properties.Replace(old, cs);
                        }
                        return state;
                    });
                    //return;
                }
            }
        }
    }

    [GeneratedRegex("^(\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}.*\\dZ)\\s(Debugger listening on)\\s(?<url>.*)$", RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex DetectDebuggerUrl();
}
