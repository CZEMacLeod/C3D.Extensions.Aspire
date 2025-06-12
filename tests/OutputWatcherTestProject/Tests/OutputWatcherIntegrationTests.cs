using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit.Abstractions;

namespace OutputWatcherTestProject.Tests;

public class OutputWatcherIntegrationTests(ITestOutputHelper outputHelper)
{
    private void WriteFunctionName([CallerMemberName] string? caller = null) => outputHelper.WriteLine(caller);
    private const int WaitForHealthyTimeoutSeconds = 90;
    private static readonly TimeSpan WaitForHealthyTimeout = TimeSpan.FromSeconds(90);

    private async Task<IDistributedApplicationTestingBuilder> CreateAppHostAsync()
    {
        WriteFunctionName();

        //System.Environment.SetEnvironmentVariable("ASPIRE_ENVIRONMENT=Test");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireAppHostWaitForConsole>([], (dab, host) =>
        {
            dab.EnableResourceLogging = true;
            host.Configuration!.AddUserSecrets<OutputWatcherIntegrationTests>();
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
            var timeout = TimeSpan.FromSeconds(appHost.Configuration.GetValue<double>("HttpClientTimeout", 30));
            clientBuilder
                .AddStandardResilienceHandler(res => {
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
    public async Task GetRootReturnsOkStatusCode()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("webapp", "http");
        await resourceNotificationService.WaitForResourceAsync("webapp", KnownResourceStates.Running).WaitAsync(WaitForHealthyTimeout);
        var response = await httpClient.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        outputHelper.WriteLine(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetNumberReturnsNumber()
    {
        WriteFunctionName();

        // Arrange
        await using Aspire.Hosting.DistributedApplication app = await ArrangeAppHostAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("webapp", "http");
        await resourceNotificationService.WaitForResourceAsync("webapp", KnownResourceStates.Running).WaitAsync(WaitForHealthyTimeout);
        var response = await httpClient.GetAsync("/number");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        
        var content = await response.Content.ReadAsStringAsync();
        outputHelper.WriteLine(content);

        Assert.True(int.TryParse(content, out var number));

        Assert.InRange(number, 0, 100);
    }
}
