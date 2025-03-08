using AspireAppHostNodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

//var console = builder.AddProject<Projects.NodeConsoleApp>("console");

var prj = new Projects.ExpressProject();
var prjPath = System.IO.Path.GetDirectoryName(prj.ProjectPath)!;

var debugPort = 9229;

var webapp = builder.AddNodeApp("webapp", ".", prjPath)
    .WithHttpEndpoint(3030, isProxied: false)
    .WithOtlpExporter()
    .WithDebugger(C3D.Extensions.Aspire.VisualStudioDebug.DebugMode.VisualStudio,
        "JavaScript and TypeScript")
    .WithArgs(c =>
     {
         c.Args.Insert(0, $"--inspect-wait={debugPort}");
         //c.Args.Insert(1, "--require");
         //c.Args.Insert(2, "./instrumentation.js");
     });

builder.Services.AddHostedService<NodeDebugHook>();


builder.Build().Run();
