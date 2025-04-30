using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Aspire.Hosting;

namespace SWATestProject.Tests;

public class SWAIntegrationTests(ITestOutputHelper outputHelper)
{
    private void WriteFunctionName([CallerMemberName] string? caller = null) => outputHelper.WriteLine(caller);
    private static TimeSpan WaitForHealthyTimeout = TimeSpan.FromSeconds(90);

    private async Task<IDistributedApplicationTestingBuilder> CreateAppHostAsync()
    {
        WriteFunctionName();

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireAppHostSWA>([], (dab, host) =>
        {
            //dab.DisableDashboard = true;
            dab.EnableResourceLogging = true;
            host.EnvironmentName = "Test";
            //dab.AssemblyName = this.GetType().Assembly.GetName().Name;
        });
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
            
            clientBuilder
                .AddStandardResilienceHandler(res=> {
                    res.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                    res.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                    res.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                });
            clientBuilder
                    .ConfigurePrimaryHttpMessageHandler(() =>
                     {
                         // Allowing Untrusted SSL Certificates
                         var handler = new HttpClientHandler();
                         handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                         handler.ServerCertificateCustomValidationCallback =
                             (httpRequestMessage, cert, cetChain, policyErrors) => true;

                         return handler;
                     });
        });
        return appHost;
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

        // Act
        var httpClient = app.CreateHttpClient("framework", "http");
        await resourceNotificationService.WaitForResourceAsync("framework", KnownResourceStates.Running).WaitAsync(WaitForHealthyTimeout);
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

        // Act
        var httpClient = app.CreateHttpClient("core", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(TimeSpan.FromSeconds(90));
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(TimeSpan.FromSeconds(90));
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

        // Act
        var httpClient = app.CreateHttpClient("framework", "http");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);
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
        
        var httpClient = app.CreateHttpClient("framework", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);

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
            var resource = app.Services.GetRequiredService<DistributedApplicationModel>().Resources.Single(r=>r.Name=="framework") as IResourceWithEndpoints;
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

        // Act
        var httpClient = app.CreateHttpClient("core", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);
        var response = await httpClient.GetAsync("/framework");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCoreSessionReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireAppHostSWA>();

        appHost
            .Services
            .AddLogging(logging => logging
                            .ClearProviders()
                            .SetMinimumLevel(LogLevel.Debug)
                            .AddDebug()
                            .AddXunit(outputHelper)
                            );

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("core", "https");
        await resourceNotificationService.WaitForResourceHealthyAsync("framework", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);
        await resourceNotificationService.WaitForResourceHealthyAsync("core", WaitBehavior.StopOnResourceUnavailable).WaitAsync(WaitForHealthyTimeout);
        var response = await httpClient.GetAsync("/core");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
