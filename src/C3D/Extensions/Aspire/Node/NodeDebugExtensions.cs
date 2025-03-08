using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.Node;
using C3D.Extensions.Aspire.VisualStudioDebug;
using C3D.Extensions.Aspire.VisualStudioDebug.WellKnown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public static class NodeDebugExtensions
{
    public static IResourceBuilder<NodeAppResource> AddNodeApp<TProject>(
        this IDistributedApplicationBuilder builder,
        string name, string path = ".", params string[] args)
        where TProject : IProjectMetadata, new()
    {
        var prj = new TProject();
        var prjPath = System.IO.Path.GetDirectoryName(prj.ProjectPath)!;

        return builder.AddNodeApp(name, path, prjPath, args);
    }

    public static IResourceBuilder<NodeAppResource> WithDebugger(this IResourceBuilder<NodeAppResource> builder)
    {
        if (builder.ApplicationBuilder.Environment.IsDevelopment() &&
            builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder.ApplicationBuilder.Services.AddHostedService<NodeDebugHook>();

            builder
                .WithEndpoint(scheme: "ws", name: "debug", isExternal: false)
                .WithOption(() => $"--inspect-wait={builder.GetEndpoint("debug").Port}")
                .WithDebugger(DebugMode.VisualStudio)
                .WithDebugTransport(Transports.V8Inspector)
                .WithDebugEngine(Engines.JavaScript);
        }
        return builder;
    }

    public static IResourceBuilder<NodeAppResource> WithWatch(this IResourceBuilder<NodeAppResource> builder)
    {
        if (builder.ApplicationBuilder.Environment.IsDevelopment() &&
            builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder.WithOption("--watch");
        }
        return builder;
    }

    private static void InsertRange<T>(this IList<T> list, int startIndex, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            list.Insert(startIndex++, value);
        }
    }

    public static IResourceBuilder<NodeAppResource> WithOption(this IResourceBuilder<NodeAppResource> builder,
        params string[] options) => builder.WithArgs(c => c.Args.InsertRange(0, options));

    public static IResourceBuilder<NodeAppResource> WithOption(this IResourceBuilder<NodeAppResource> builder,
        params Func<string>[] options) => builder.WithArgs(c => 
            c.Args.InsertRange(0, options.Select(o=>o())));
}
