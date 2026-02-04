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
        .WithHttpsEndpoint(env: "HTTPS_PORT")
        .WithHttpsRedirctionPort("HTTPS_REDIRECT_PORT")
        .RunWithHttpsDevCertificate("HTTPS_CERT_FILE", "HTTPS_CERT_KEY_FILE");
}


builder.Build().Run();
