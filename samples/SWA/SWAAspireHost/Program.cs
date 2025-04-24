using C3D.Extensions.Aspire.IISExpress;
using Microsoft.Extensions.Hosting;


var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions()
{
    Args = args,
    AllowUnsecuredTransport = true
});

var framework = builder.AddIISExpressProject<Projects.SWAFramework>("framework", IISExpressBitness.IISExpress64Bit)
    .WithIISExpressDeveloperCertificate()
    .WithSystemWebAdapters()
    .WithHttpHealthCheck("/debug", 204)
    .WithUrlForEndpoint("http", u =>
    {
        u.DisplayText = "Framework (http)";
        u.DisplayOrder = 12;
    })
    .WithUrlForEndpoint("https", u =>
    {
        u.DisplayText = "Framework (https)";
        u.DisplayOrder = 22;
    })
    .WithEnvironment(e =>
    {
        if (e.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
        {
            e.EnvironmentVariables["WaitForDebugger"] = "true";
        }
    })
    .WithUrls(u =>
    {
        if (u.Resource.TryGetEndpoints(out var eps))
        {
            foreach (var ep in eps)
            {
                u.Urls.Add(new()
                {
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "debug" }.ToString(),
                    DisplayText = $"Debugger ({ep.Name})",
                    DisplayOrder = 11 + (ep.Name == "https" ? 10 : 0)
                }
                );
                u.Urls.Add(new()
                {
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "framework" }.ToString(),
                    DisplayText = $"Framework Session ({ep.Name})",
                    DisplayOrder = 10 + (ep.Name == "https" ? 10 : 0)
                }
                );
            }
        }
    });

builder.AddProject<Projects.SWACore>("core")
    .RunWithIISExpressDeveloperCertificate("ASPNETCORE_Kestrel__Certificates__Default__Path", "ASPNETCORE_Kestrel__Certificates__Default__KeyPath")
    .WithSystemWebAdapters(framework)
    .WithHttpsHealthCheck("/alive")
    .WithUrlForEndpoint("http", u =>
    {
        u.DisplayText = "Core (http)";
        u.DisplayOrder = 12;
    })
    .WithUrlForEndpoint("https", u =>
    {
        u.DisplayText = "Core (https)";
        u.DisplayOrder = 22;
    })
    .WithUrls(u =>
    {
        if (u.Resource.TryGetEndpoints(out var eps))
        {
            foreach (var ep in eps)
            {
                u.Urls.Add(new()
                {
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "core" }.ToString(),
                    DisplayText = $"Core Session ({ep.Name})",
                    DisplayOrder = 11 + (ep.Name == "https" ? 10 : 0)
                }
                );
                u.Urls.Add(new()
                {
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "framework" }.ToString(),
                    DisplayText = $"Framework Session ({ep.Name})",
                    DisplayOrder = 10 + (ep.Name == "https" ? 10 : 0)
                }
                );
            }
        }
    })
    .WithUrl("Framework", "Framework Session");

builder.Build().Run();
