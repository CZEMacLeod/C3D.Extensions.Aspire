using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.Node;
using C3D.Extensions.Aspire.Node.Annotations;
using C3D.Extensions.Aspire.OutputWatcher;
using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.Aspire.VisualStudioDebug.Annotations;
using C3D.Extensions.Aspire.VisualStudioDebug.WellKnown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Aspire.Hosting;

public static partial class NodeDebugExtensions
{
    private const string node_options_env = "NODE_OPTIONS";

    public static IResourceBuilder<NodeAppResource> AddNodeApp<TProject>(
        this IDistributedApplicationBuilder builder,
        string name, string path = ".", params string[] args)
        where TProject : IProjectMetadata, new()
    {
        var prj = new TProject();
        var prjPath = System.IO.Path.GetDirectoryName(prj.ProjectPath)!;

        return builder.AddNodeApp(name, path, prjPath, args);
    }

    //public static IResourceBuilder<NodeAppResource> AddNpmApp<TProject>(
    //    this IDistributedApplicationBuilder builder,
    //    string name, string scriptName = "start", params string[] args)
    //where TProject : IProjectMetadata, new()
    //{
    //    var prj = new TProject();
    //    var prjPath = System.IO.Path.GetDirectoryName(prj.ProjectPath)!;

    //    return builder.AddNpmApp(name, prjPath, scriptName, args);
    //}

    public static IResourceBuilder<NodeAppResource> WithNodeOption(this IResourceBuilder<NodeAppResource> builder,
        string option) =>
        builder.WithNodeOptions().WithAnnotation(NodeOptionAnnotation.Create(option));

    public static IResourceBuilder<NodeAppResource> WithNodeOption(this IResourceBuilder<NodeAppResource> builder,
        ReferenceExpression option) =>
        builder.WithNodeOptions().WithAnnotation(NodeOptionAnnotation.Create(option));

    private static IResourceBuilder<NodeAppResource> WithNodeOptions(this IResourceBuilder<NodeAppResource> builder)
    {
        var resource = builder.Resource;
        // If there are already any options, don't add the environment variable again or it will duplicate.
        if (resource.HasAnnotationOfType<NodeOptionAnnotation>()) return builder;

        return builder.WithEnvironment(c =>
        {
            if (resource.TryGetAnnotationsOfType<NodeOptionAnnotation>(out var options))
            {
                var resourceBuilder = new ReferenceExpressionBuilder();
                foreach (var option in options)
                {
                    AppendSeperator(resourceBuilder);

                    resourceBuilder.AppendFormatted(option);
                }

                if (c.EnvironmentVariables.TryGetValue(node_options_env, out var no))
                {
                    switch (no)
                    {
                        case null:
                            break;
                        case string s:
                            AppendSeperator(resourceBuilder);

                            resourceBuilder.AppendLiteral(s);
                            break;
                        case ReferenceExpression expression:
                            AppendSeperator(resourceBuilder);

                            resourceBuilder.AppendFormatted(expression);
                            break;
                        default:
                            c.Logger.LogError("{env} already set and could not append type {type}.\r\nOverwriting old value.\r\n{old}",
                                node_options_env,
                                no.GetType().Name,
                                no.ToString());
                            break;
                    }
                }
                c.EnvironmentVariables[node_options_env] = resourceBuilder.Build();
            }
        });
    }

    private static void AppendSeperator(ReferenceExpressionBuilder resourceBuilder)
    {
        if (!resourceBuilder.IsEmpty)
            resourceBuilder.AppendLiteral(" ");
    }

    private const string connectionStringPropertyName = "debug.v8.connectionString";
    private const string debugWatcherKey = "debugUrl";

    public static IResourceBuilder<NodeAppResource> WithDebugger(this IResourceBuilder<NodeAppResource> builder)
    {
        if (builder.ApplicationBuilder.Environment.IsDevelopment() &&
            builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            //builder.ApplicationBuilder.Services.AddHostedService<NodeDebugHook>();

            var ep = builder.GetEndpoint("debug").Property(EndpointProperty.TargetPort);
            var inspect = ReferenceExpression.Create($"--inspect-wait={ep}");

            builder
                .WithEndpoint(scheme: "ws", name: "debug", isExternal: false)
                .WithNodeOption(inspect)
                .WithDebugger(DebugMode.VisualStudio)
                .WithDebugEngine(Engines.JavaScript)
                .WithDebugSkip()    // we skip debugging until we have applied the transport and qualifier
                .WithDebuggerHealthcheck()
                .WithOutputWatcher(GetDebugRegex(), key: debugWatcherKey)
                ;

            builder.ApplicationBuilder.Eventing.Subscribe<OutputMatchedEvent>(UpdateDebugInformation);
        }
        return builder;
    }

    private static async Task UpdateDebugInformation(OutputMatchedEvent @event, CancellationToken token)
    {
        if (@event.Key == debugWatcherKey)
        {
            var url = @event.Properties["url"].ToString()!;
            var debugAnnotation = @event.Resource.Annotations.OfType<DebugAttachAnnotation>().Last();
            var logger = @event.ServiceProvider.GetRequiredService<ILogger<NodeAppResource>>();
            logger.LogInformation("Debugger connection string {url}", url);
            if (debugAnnotation.DebuggerProcessId is not null)
            {
                logger.LogWarning("Previously Debugged");
                debugAnnotation.DebuggerProcessId = null;
            }
            @event.Resource.Annotations.Add(new DebugAttachTransportAnnotation()
            {
                Transport = C3D.Extensions.VisualStudioDebug.WellKnown.Transports.V8Inspector,
                Qualifier = url
            });
            debugAnnotation.Skip = false;
            var resourceNotificationService = @event.ServiceProvider.GetRequiredService<ResourceNotificationService>();
            await resourceNotificationService.PublishUpdateAsync(@event.Resource, state =>
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
        }

    }

    [GeneratedRegex("^(?:(Debugger listening on))\\s(?<url>.*)$", RegexOptions.ExplicitCapture | RegexOptions.Singleline)]
    private static partial Regex GetDebugRegex();

    public static IResourceBuilder<NodeAppResource> WithWatch(this IResourceBuilder<NodeAppResource> builder)
    {
        if (builder.ApplicationBuilder.Environment.IsDevelopment() &&
            builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder.WithArgs(c =>
            {
                c.Args.Insert(0, "--watch");
            });
            //builder.WithNodeOption("--watch");    // This doesn't work as expected just now
        }
        return builder;
    }
}
