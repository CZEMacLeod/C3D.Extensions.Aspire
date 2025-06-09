using Aspire.Hosting.Publishing;
using AspireAppHostSWA;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions()
{
    Args = args,
    AllowUnsecuredTransport = true
});

var env = builder
    .AddDockerComposeEnvironment("swa-env")
    .ConfigureComposeFile(configure =>
    {
        //configure.Version = "3.9"; // Specify the Docker Compose file version
        configure.Networks["aspire"].Driver = "nat";
        //configure.AddService(new()
        //{
        //    Name = "sql",
        //    Image = "mcr.microsoft.com/mssql/server:2022-latest",
        //    Environment =
        //    {
        //        {"ACCEPT_EULA", "Y" },
        //        {"SA_PASSWORD", "YourStrong@Passw0rd"}
        //    },
        //    Ports = { "1433:1433" },
        //    Restart = "always"
        //});
    });


//var framework = builder.AddIISExpress("iis")
//    .AddSiteProject<Projects.SWAFramework>("framework")

#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

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
        u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
    })
    .WithUrlForEndpoint("https", u =>
    {
        u.DisplayText = "Framework (https)";
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
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "debug" }.ToString(),
                    DisplayText = $"Debugger ({ep.Name})",
                    DisplayOrder = 11 + (ep.Name == "https" ? 10 : 0),
                    DisplayLocation = ep.Name == "https" ? UrlDisplayLocation.SummaryAndDetails : UrlDisplayLocation.DetailsOnly
                }
                );
                u.Urls.Add(new()
                {
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "framework" }.ToString(),
                    DisplayText = $"Framework Session ({ep.Name})",
                    DisplayOrder = 10 + (ep.Name == "https" ? 10 : 0),
                    DisplayLocation = ep.Name == "https" ? UrlDisplayLocation.SummaryAndDetails : UrlDisplayLocation.DetailsOnly
                }
                );
            }
        }
    })
    .WhenDebugMode(r => r.WithHttpHealthCheck("/debug", 204)
                         .WithEnvironment("WaitForDebugger", "true"))
    .WhenUnderTest(r => r.WithTemporaryConfig()    // Ensure we use a temp config with random port numbers
                         .WithDefaultIISExpressEndpoints()
                   //,r => r.WithTemporaryConfig()
                   )
    .WithOtlpProtocol("http/protobuf")
    .PublishAsDockerFile(configure =>
        {
            configure.WithPublishingCallback(async pc =>
            {
                pc.Logger.LogInformation("Publishing {ResourceName} as Dockerfile", configure.Resource.Name);
            });
        })
    .WhenPublishMode(r =>
    {
        r = r.WithTemporaryConfig()
            .WithDefaultIISExpressEndpoints(80, 443);

        builder.Eventing.Subscribe<BeforePublishEvent>(async (e, c) =>
        {
            var activityReporter = e.Services.GetRequiredService<IPublishingActivityProgressReporter>();
            var cancellationToken = c;
            var build = e.Services.GetRequiredService<C3D.Extensions.Aspire.IISExpress.CommandExecutor>();
            var logger = e.Services.GetRequiredService<ILogger<Program>>();


            //var logger = e.Logger;
            var options = e.Services.GetRequiredService<IOptions<PublishingOptions>>();

            if (string.IsNullOrEmpty(options.Value.OutputPath))
            {
                logger.LogError("The '--output-path [path]' option was not specified.");
                throw new DistributedApplicationException(
                    "The '--output-path [path]' option was not specified."
                );
            }
            var outputPath = Path.Combine(options.Value.OutputPath, r.Resource.Name);

            logger.LogInformation("Publishing {ResourceName} to {outputPath}", r.Resource.Name, outputPath);
            await PublishIISResourceAsync(r, r.Resource.Name, activityReporter, build, logger, outputPath, cancellationToken);
        });

        //var resourceName = r.Resource.Name; // "framework" in this case
        //var annotation = new PublishingCallbackAnnotation(async pc =>
        //{
        //    var activityReporter = pc.Services.GetRequiredService<IPublishingActivityProgressReporter>();
        //    var cancellationToken = pc.CancellationToken;
        //    var build = pc.Services.GetRequiredService<C3D.Extensions.Aspire.IISExpress.CommandExecutor>();
        //    var logger = pc.Logger;
        //    var outputPath = Path.Combine(pc.OutputPath, resourceName);

        //    await PublishIISResourceAsync(r, resourceName, activityReporter, build, logger, outputPath, cancellationToken);
        //});

        //r.WithAnnotation(annotation);

        return r;
    })

    .PublishAsDockerComposeService((r, service) =>
    {
        service.Restart = "always";
    })

    //.WithPublishingCallback(async pc =>
    //{
    //    var build = pc.Services.GetRequiredService<C3D.Extensions.Aspire.IISExpress.CommandExecutor>();
    //    var r = pc.Model.Resources.Single(r => r.Name == "framework");
    //    var pm = r.Annotations.OfType<IProjectMetadata>().Single();
    //    pc.Logger.LogInformation("Publishing {ResourceName} to {OutputPath}", r.Name, pc.OutputPath);

    //    await build.ExecuteCommandAsync("msbuild",
    //        pm.ProjectPath,
    //        "/t:rebuild",
    //        "/p:Configuration=Release",
    //        "/p:DeployOnBuild=True",
    //        "/p:DeployDefaultTarget=WebPublish",
    //        "/p:WebPublishMethod=FileSystem",
    //        "/p:DeleteExistingFiles=True",
    //        $"/p:publishUrl=\"{pc.OutputPath}\\{r.Name}\"");
    //})
    //.PublishAsDockerFile(configure =>
    //{
    //    configure.WithPublishingCallback(async pc =>
    //    {
    //        var build = pc.Services.GetRequiredService<C3D.Extensions.Aspire.IISExpress.CommandExecutor>();
    //        var r = pc.Model.Resources.Single(r => r.Name == "framework");
    //        var pm = r.Annotations.OfType<IProjectMetadata>().Single();
    //        await build.ExecuteCommandAsync("msbuild",
    //            pm.ProjectPath,
    //            "/t:rebuild",
    //            "/p:Configuration=Release",
    //            "/p:DeployOnBuild=True",
    //            "/p:DeployDefaultTarget=WebPublish",
    //            "/p:WebPublishMethod=FileSystem",
    //            "/p:DeleteExistingFiles=True",
    //            $"/p:publishUrl=\"{pc.OutputPath}\\{r.Name}\"");
    //    });
    //})
    .WithComputeEnvironment(env)
//.PublishAsDockerComposeService((r, service) =>
//{
//    service.Image = "swaframework:dev"; // Specify the image name for the Docker Compose service
//    //var rb = builder.CreateResourceBuilder(r);
//    //var p = rb.t
//    //var annotation = new DockerfileBuildAnnotation(builder.AppHostDirectory, rb.r);

//    //rb.WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Replace);
//    service.Restart = "always";
//})

;
;

var core = builder.AddProject<Projects.SWACore>("core")
    //.WithSystemWebAdapters(framework)   // Use this __or__ the AddSystemWebAdapters method below
    .WithHttpHealthCheck("/alive", endpointName: "https")
    .WithUrlForEndpoint("http", u =>
    {
        u.DisplayText = "Core (http)";
        u.DisplayOrder = 12;
        u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
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
                    DisplayOrder = 11 + (ep.Name == "https" ? 10 : 0),
                    DisplayLocation = ep.Name == "https" ? UrlDisplayLocation.SummaryAndDetails : UrlDisplayLocation.DetailsOnly
                }
                );
                u.Urls.Add(new()
                {
                    Url = new UriBuilder(ep.AllocatedEndpoint!.UriString) { Path = "framework" }.ToString(),
                    DisplayText = $"Framework Session ({ep.Name})",
                    DisplayOrder = 10 + (ep.Name == "https" ? 10 : 0),
                    DisplayLocation = ep.Name == "https" ? UrlDisplayLocation.SummaryAndDetails : UrlDisplayLocation.DetailsOnly
                }
                );
            }
        }
    })
    .WithUrl("Framework", "Framework Session")
    .WithComputeEnvironment(env)
    .PublishAsDockerComposeService((r, service) =>
    {
        //var rb = builder.CreateResourceBuilder(r);
        //if (r.TryGetLastAnnotation<IProjectMetadata>(out var pm))
        //{
        //    var contextPath = "..\\..\\.."; // Default context path relative to the project directory
        //    var dockerfilePath = "Dockerfile";

        //    var fullyQualifiedContextPath = Path.GetFullPath(contextPath, rb.ApplicationBuilder.AppHostDirectory);
        //    var fullyQualifiedDockerfilePath = Path.GetFullPath(dockerfilePath, pm.ProjectPath);
        //    var annotation = new DockerfileBuildAnnotation(fullyQualifiedContextPath, fullyQualifiedDockerfilePath, "final");
        //    rb.WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Replace);
        //}

        service.Restart = "always";
    })
    //.PublishAsDockerFile(configure =>
    //{
    //    if (configure.Resource.TryGetLastAnnotation<IProjectMetadata>(out var pm))
    //    {
    //        var contextPath = "..\\..\\.."; // Default context path relative to the project directory
    //        var dockerfilePath = "Dockerfile";

    //        var fullyQualifiedContextPath = Path.GetFullPath(contextPath, configure.ApplicationBuilder.AppHostDirectory);
    //        var fullyQualifiedDockerfilePath = Path.GetFullPath(dockerfilePath, pm.ProjectPath);
    //        var annotation = new DockerfileBuildAnnotation(fullyQualifiedContextPath, fullyQualifiedDockerfilePath, "final");
    //        configure.WithAnnotation(annotation, ResourceAnnotationMutationBehavior.Replace);
    //    }
    //})
    ;

// New way to do SystemWebAdapters instead of WithSystemWebAdapters.
var swa = builder
    .AddSystemWebAdapters("swa")
    .WithFramework(framework)
    .WithCore(core);

builder.AddDashboard();

builder.Build().Run();

static async Task PublishIISResourceAsync(IResourceBuilder<C3D.Extensions.Aspire.IISExpress.Resources.IISExpressProjectResource> r,
    string resourceName,
    IPublishingActivityProgressReporter activityReporter,
    C3D.Extensions.Aspire.IISExpress.CommandExecutor build,
    ILogger logger,
    string outputPath,
    CancellationToken cancellationToken)
{
    var publishingActivity = await activityReporter.CreateActivityAsync(
                    $"{resourceName}-publish-iis-resource",
                    $"Publishing Files: {resourceName}",
                    isPrimary: false,
                    cancellationToken
                    ).ConfigureAwait(false);

    (_, var vs_path) = await build.GetCommandOutputAsync("vswhere.exe", "-latest -property installationPath", false);
    var vs_tools_path = Path.Combine(vs_path.Trim(), "MSBuild", "Microsoft", "VisualStudio", "v17.0");
    var web_applications_targets_path = Path.Combine(vs_tools_path, "WebApplications", "Microsoft.WebApplication.targets");
    logger.LogInformation("Using WebApplications targets path: {WebApplicationsTargetsPath}", web_applications_targets_path);
    //var r = pc.Model.Resources.Single(r => r.Name == "framework");
    if (r.Resource.TryGetLastAnnotation<IProjectMetadata>(out var pm))
    {
        logger.LogInformation("Publishing {ResourceName} to {OutputPath}", resourceName, outputPath);
        await activityReporter.UpdateActivityStatusAsync(publishingActivity, status =>
        {
            return status with
            {
                StatusText = $"Publishing {resourceName} to {outputPath}",
                IsComplete = false,
                IsError = false
            };
        }, cancellationToken).ConfigureAwait(false);

        Directory.CreateDirectory(outputPath); // Ensure the output directory exists

        await build.ExecuteCommandAsync("dotnet",
            logger,
            cancellationToken,
            new Dictionary<string, string?>()
            {
                        { "DOTNET_NOLOGO", "1" },
                        { "DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1" },
                        { "DOTNET_CLI_TELEMETRY_OPTOUT", "1" },
                        { "MSBUILDENSURESTDOUTFORTASKPROCESSES", "1" }
            },
            "msbuild",
            pm.ProjectPath,
            //"-interactive:False",
            //"-verbosity:detailed",
            "-verbosity:minimal",
            "-terminalLogger:off",
            "/t:build",
            "/p:Configuration=Release",
            "/p:DeployOnBuild=True",
            "/p:DeployDefaultTarget=WebPublish",
            "/p:WebPublishMethod=FileSystem",
            "/p:DeleteExistingFiles=True",
            "/p:MvcBuildViews=False",
            //"/p:PrecompileBeforePublish=true",
            $"/p:WebApplicationsTargetPath={web_applications_targets_path}",
            $"/p:publishUrl={outputPath}");
        logger.LogInformation("Creating Dockerfile in {OutputPath}", outputPath);

        using var fileStream = new FileStream(Path.Combine(outputPath, "Dockerfile"), FileMode.OpenOrCreate, FileAccess.Write);
        using var writer = new StreamWriter(fileStream);
        writer.WriteLine("FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019");
        writer.WriteLine("WORKDIR /inetpub/wwwroot");
        writer.WriteLine("COPY . .");


        r.WithAnnotation(new DockerfileBuildAnnotation(outputPath, Path.Combine(outputPath, "Dockerfile"), null));

        await activityReporter.UpdateActivityStatusAsync(publishingActivity, status =>
        {
            return status with
            {
                StatusText = $"Publishing {resourceName} to {outputPath}",
                IsComplete = true,
                IsError = false
            };
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Publishing {ResourceName} Complete", resourceName);
    }
    else
    {
        logger.LogWarning("No project metadata found for resource {ResourceName}. Skipping publish.", r.Resource.Name);
        await activityReporter.UpdateActivityStatusAsync(publishingActivity, status =>
        {
            return status with
            {
                StatusText = $"No project metadata found for resource {resourceName}. Skipping publish.",
                IsComplete = true,
                IsError = true
            };
        }, cancellationToken).ConfigureAwait(false);
    }
}