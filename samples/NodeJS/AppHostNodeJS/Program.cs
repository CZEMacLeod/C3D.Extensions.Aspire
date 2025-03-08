using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var webapp = builder.AddNodeApp<Projects.ExpressProject>("webapp")
    .WithHttpEndpoint(env: "PORT")
    .WithOtlpExporter()
    .WithWatch()
    .WithDebugger()
    .WithHttpHealthCheck("/alive");

var launchProfile = builder.Configuration["DOTNET_LAUNCH_PROFILE"] ??
                    builder.Configuration["AppHost:DefaultLaunchProfileName"]; // work around https://github.com/dotnet/aspire/issues/5093

if (builder.Environment.IsDevelopment() && launchProfile == "https")
{
    webapp
        .RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE")
        .WithHttpsHealthCheck("/alive"); ;
}


builder.Build().Run();
