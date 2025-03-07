using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

//var console = builder.AddProject<Projects.NodeConsoleApp>("console");

var prj = new Projects.ExpressProject();
var prjPath = System.IO.Path.GetDirectoryName(prj.ProjectPath)!;
builder.AddNodeApp("webapp", ".", prjPath)
    .WithHttpEndpoint(3030, isProxied: false)
    .WithArgs("--inspect")
    .WithDebugger(C3D.Extensions.Aspire.VisualStudioDebug.DebugMode.VisualStudio, 
        "JavaScript and TypeScript")
    //.WaitForCompletion(console)
    ;

builder.Build().Run();
