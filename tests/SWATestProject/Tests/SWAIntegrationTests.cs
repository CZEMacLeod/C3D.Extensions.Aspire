using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SWATestProject.Fixtures;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit.Abstractions;

namespace SWATestProject.Tests;

public class SWAIntegrationTests(ITestOutputHelper outputHelper, NetworkingFixture networkingFixture) : IClassFixture<NetworkingFixture>
{
    private void WriteFunctionName([CallerMemberName] string? caller = null) => outputHelper.WriteLine(caller);
    private const int WaitForHealthyTimeoutSeconds = 180;    
    private static readonly TimeSpan WaitForHealthyTimeout = TimeSpan.FromSeconds(WaitForHealthyTimeoutSeconds);

    private C3D.Extensions.Networking.PortAllocator PortAllocator => networkingFixture.PortAllocator;

    private async Task<IDistributedApplicationTestingBuilder> CreateAppHostAsync()
    {
        WriteFunctionName();

        //System.Environment.SetEnvironmentVariable("ASPIRE_ENVIRONMENT=Test");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireAppHostSWA>([], (dab, host) =>
        {
            //dab.DisableDashboard = true;
            dab.EnableResourceLogging = true;
            //dab.AllowUnsecuredTransport = true;

            //ConfigureOtlpOverHttp(host, outputHelper.WriteLine);

            host.Configuration!.AddUserSecrets<SWAIntegrationTests>();
            //host.EnvironmentName = "Test";
            //dab.AssemblyName = this.GetType().Assembly.GetName().Name;
        });

        ConfigureOtlpOverHttp(appHost.Configuration, outputHelper.WriteLine);

        appHost
            .Services
            .AddLogging(logging => logging
                            .ClearProviders()
                            .SetMinimumLevel(LogLevel.Debug)
                            .AddDebug()
                            .AddXunit(outputHelper, c => c.TimeStamp = C3D.Extensions.Xunit.Logging.XunitLoggerTimeStamp.Offset)
                            );

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            var timeout = TimeSpan.FromSeconds(appHost.Configuration.GetValue<double>("HttpClientTimeout", 30));
            clientBuilder
                .AddStandardResilienceHandler(res =>
                {
                    res.TotalRequestTimeout.Timeout = timeout * 4;
                    res.CircuitBreaker.SamplingDuration = timeout * 2;
                    res.AttemptTimeout.Timeout = timeout;
                });
            clientBuilder
                    .ConfigurePrimaryHttpMessageHandler(() =>
                     {
                         // Allowing Untrusted SSL Certificates
                         var handler = new HttpClientHandler
                         {
                             ClientCertificateOptions = ClientCertificateOption.Manual,
                             ServerCertificateCustomValidationCallback =
                                 (httpRequestMessage, cert, cetChain, policyErrors) => true
                         };

                         return handler;
                     });
        });

        return appHost;
    }

    private void ConfigureOtlpOverHttp(HostApplicationBuilderSettings host, Action<string>? logMessage = null) =>
        ConfigureOtlpOverHttp(host.Configuration!, logMessage);

    private void ConfigureOtlpOverHttp(ConfigurationManager configuration, Action<string>? logMessage = null)
    {
        var port = PortAllocator.GetRandomFreePort(20000, 30000);
        var otlp = new UriBuilder("https", "localhost", port).ToString();
        logMessage?.Invoke($"Setting OTLP endpoint to {otlp}");
        var dict = new Dictionary<string, string?>()
        {
            { "DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL" , otlp },
            { "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL" , null }
        };
        configuration.AddInMemoryCollection(dict);
    }

    private async Task<Aspire.Hosting.DistributedApplication> ArrangeAppHostAsync()
    {
        WriteFunctionName();

        var appHost = await CreateAppHostAsync();
        var app = await appHost.BuildAsync();
        return app;
    }


    [Fact]
    public async Task AppHostBuildsOkay()
    {
        WriteFunctionName();
        // Arrange
        await using var appHost = await CreateAppHostAsync();
        // Act
        await using Aspire.Hosting.DistributedApplication app = await appHost.BuildAsync();
        // Assert
        Assert.NotNull(app);
        // Clean Up
        await app.DisposeAsync();
        await appHost.DisposeAsync();
    }

    [Fact]
    public async Task AppHostStartsOkay()
    {
        WriteFunctionName();
        // Arrange
        await using var app = await ArrangeAppHostAsync();
        // Act
        await app.StartAsync();
        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public async Task GetFrameworkRootReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
        var timeout = TimeSpan.FromSeconds(app.Services.GetRequiredService<IConfiguration>().GetValue<double>("WaitForHealthyTimeoutSeconds", WaitForHealthyTimeoutSeconds));

        // Act
        var httpClient = app.CreateHttpClient("framework", "http");
        await resourceNotificationService.WaitForResourceAsync("framework", KnownResourceStates.Running).WaitAsync(timeout);
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCoreRootReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
        var timeout = TimeSpan.FromSeconds(app.Services.GetRequiredService<IConfiguration>().GetValue<double>("WaitForHealthyTimeoutSeconds", WaitForHealthyTimeoutSeconds));


        // Act
        var httpClient = app.CreateHttpClient("core", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFrameworkSessionReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
        var timeout = TimeSpan.FromSeconds(app.Services.GetRequiredService<IConfiguration>().GetValue<double>("WaitForHealthyTimeoutSeconds", WaitForHealthyTimeoutSeconds));

        // Act
        var httpClient = app.CreateHttpClient("framework", "http");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        var response = await httpClient.GetAsync("/framework");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact(Skip = "Requires Admin")]
    //[Fact]
    public async Task GetFrameworkHttpsWorksSometimes()
    {
        WriteFunctionName();
        // Arrange
        await using var appHost = await CreateAppHostAsync();
        await using Aspire.Hosting.DistributedApplication app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
        var timeout = TimeSpan.FromSeconds(app.Services.GetRequiredService<IConfiguration>().GetValue<double>("WaitForHealthyTimeoutSeconds", WaitForHealthyTimeoutSeconds));

        var httpClient = app.CreateHttpClient("framework", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);

        try
        {
            // Act
            var response = await httpClient.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            outputHelper.WriteLine(ex.Message);

            // Fix
            var resource = app.Services.GetRequiredService<DistributedApplicationModel>().Resources.Single(r => r.Name == "framework") as IResourceWithEndpoints;
            Assert.NotNull(resource);
            await resource.ExecuteFixHttpsCommand(app.Services);

            // Act
            var response = await httpClient.GetAsync("/");
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetProxiedFrameworkSessionReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
        var timeout = TimeSpan.FromSeconds(app.Services.GetRequiredService<IConfiguration>().GetValue<double>("WaitForHealthyTimeoutSeconds", WaitForHealthyTimeoutSeconds));


        // Act
        var httpClient = app.CreateHttpClient("core", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        var response = await httpClient.GetAsync("/framework");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCoreSessionReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
        var timeout = TimeSpan.FromSeconds(app.Services.GetRequiredService<IConfiguration>().GetValue<double>("WaitForHealthyTimeoutSeconds", WaitForHealthyTimeoutSeconds));


        // Act
        var httpClient = app.CreateHttpClient("core", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(timeout);
        var response = await httpClient.GetAsync("/core");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
