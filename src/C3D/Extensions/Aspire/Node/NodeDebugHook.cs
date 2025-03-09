using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace C3D.Extensions.Aspire.Node;

partial class NodeDebugHook : BackgroundService
{
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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = model.GetExecutableResources()
            .OfType<NodeAppResource>()
            .Where(r=>r.HasAnnotationOfType<VisualStudioDebug.Annotations.DebugAttachAnnotation>())
            .Select(r => WatchResourceAsync(r, stoppingToken))
            .ToArray();
        Task.WaitAll(tasks, stoppingToken);
        return Task.CompletedTask;
    }

    private async Task WatchResourceAsync(NodeAppResource resource, CancellationToken stoppingToken)
    {
        logger.LogInformation("Waiting for debug connection string for {resource}", resource.Name);

        var regex = DetectDebuggerUrl();
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
                    resource.Annotations.Add(new DebugAttachTransportAnnotation()
                    {
                        Transport = "JavaScript and TypeScript (Chrome DevTools/V8 Inspector)",
                        Qualifier = url
                    });
                    return;
                }
            }
        }
    }

    [GeneratedRegex("^(\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}.*\\dZ)\\s(Debugger listening on)\\s(?<url>.*)$", RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex DetectDebuggerUrl();
}
