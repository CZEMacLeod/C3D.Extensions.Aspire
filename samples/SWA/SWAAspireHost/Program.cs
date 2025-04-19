using C3D.Extensions.Aspire.IISExpress;
using Microsoft.Extensions.Hosting;


var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions()
{
    Args = args,
    AllowUnsecuredTransport = true
});

var framework = builder.AddIISExpressProject<Projects.SWAFramework>("framework", IISExpressBitness.IISExpress64Bit)
    .WithSystemWebAdapters()
    .WithHttpHealthCheck("/debug", 204)
    .WithEnvironment(e=>
    {
        if (e.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
        {
            e.EnvironmentVariables["WaitForDebugger"] = "true";
        }
    })
    .WithUrl("Framework", "Framework Session");

builder.AddProject<Projects.SWACore>("core")
    .WithSystemWebAdapters(framework)
    .WithHttpsHealthCheck("/alive")
    .WithUrl("Core", "Core Session")
    .WithUrl("Framework", "Framework Session");

builder.Build().Run();
