using Aspire.Hosting;
using AspireAppHostNodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

//var console = builder.AddProject<Projects.NodeConsoleApp>("console");

var prj = new Projects.ExpressProject();
var prjPath = System.IO.Path.GetDirectoryName(prj.ProjectPath)!;

var debugPort = 9229;

var webapp = builder.AddNodeApp("webapp", ".", prjPath)
    .WithHttpEndpoint(env: "PORT")
    .WithOtlpExporter()
    .WithDebugger(C3D.Extensions.Aspire.VisualStudioDebug.DebugMode.VisualStudio)
    .WithDebugTransport(C3D.Extensions.Aspire.VisualStudioDebug.WellKnown.Transports.V8Inspector)
    .WithDebugEngine(C3D.Extensions.Aspire.VisualStudioDebug.WellKnown.Engines.JavaScript)
    .WithArgs(c =>
     {
         c.Args.Insert(0, $"--inspect-wait={debugPort}");
         c.Args.Insert(1, "--watch");
     })
    .WithHttpHealthCheck("alive");

builder.Services.AddHostedService<NodeDebugHook>();

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"] ??
                    builder.Configuration["AppHost:DefaultLaunchProfileName"]; // work around https://github.com/dotnet/aspire/issues/5093

if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
    webapp.RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE");
}


builder.Build().Run();
