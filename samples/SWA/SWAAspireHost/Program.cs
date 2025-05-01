using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions()
{
    Args = args,
    AllowUnsecuredTransport = true
});

//var framework = builder.AddIISExpress("iis")
//    .AddSiteProject<Projects.SWAFramework>("framework")

var framework = builder.AddIISExpressProject<Projects.SWAFramework>("framework")
    //.WithConfigLocation("test.config")  // use a custom config file - will be created if it doesn't exist


    //.WithTemporaryConfig()              // Use a temporary config file each time

    // Note that the config file ports will be used as the default target ports for the endpoints.
    // There will be a randomly assigned proxy port for each endpoint.
    //.WithDefaultIISExpressEndpoints()   // Allocate http and https endpoints - the ports will be allocated in the default ranges for IIS Express
    //                                    // If your config file exists and has http and https endpoints, the ports from the config will be used.

    //.WithHttpsEndpoint(name: "custom-https", targetPort: 40376)   // This will be added to the config file and saved in a temporary location

    //.WithSystemWebAdapters()            // Use this __or__ the AddSystemWebAdapters method below
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
    })
    .WhenDebugMode(r => r.WithHttpHealthCheck("/debug", 204))
    .WhenUnderTest(r => r.WithTemporaryConfig()    // Ensure we use a temp config with random port numbers
                        .WithDefaultIISExpressEndpoints())
    ;

var core = builder.AddProject<Projects.SWACore>("core")
    //.WithSystemWebAdapters(framework)   // Use this __or__ the AddSystemWebAdapters method below
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

// New way to do SystemWebAdapters instead of WithSystemWebAdapters.
var swa = builder
    .AddSystemWebAdapters("swa")
    .WithFramework(framework)
    .WithCore(core);

builder.Build().Run();
