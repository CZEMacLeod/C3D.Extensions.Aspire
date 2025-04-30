using Microsoft.AspNetCore.SystemWebAdapters;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SWACore;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Setup logging to be exported via OpenTelemetry
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddProcessRuntimeDetector();
                resource.AddAssemblyMetadataDetector();
            });

        // Add Metrics for ASP.NET Core and our custom metrics and export via OTLP
        otel.WithMetrics(metrics =>
        {
            // Metrics provider from OpenTelemetry
            metrics.AddAspNetCoreInstrumentation();
            // Metrics provides by ASP.NET Core in .NET 8
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        });

        // Add Tracing for ASP.NET Core and our custom ActivitySource and export via OTLP
        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddSource("Yarp.ReverseProxy");
        });

        // Export OpenTelemetry data via OTLP, using env vars for the configuration
        var OtlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (OtlpEndpoint != null)
        {
            otel.UseOtlpExporter();
        }

        builder.Services.AddHealthChecks()
            .AddResourceUtilizationHealthCheck()
            .AddApplicationLifecycleHealthCheck("live");

        builder.Services.AddReverseProxy();

        builder.Services.AddOptions<RemoteAppClientOptions>()
            .BindConfiguration("RemoteApp")
            .ValidateOnStart();

        builder.Services.AddSystemWebAdapters()
            .AddSessionSerializer(options =>
            {
                options.ThrowOnUnknownSessionKey = false;
            })
            .AddJsonSessionSerializer(options =>
            {
                options.RegisterKey<int>("CoreCount");
            })
            .AddRemoteAppClient(_ => { })
            .AddSessionClient();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler(resiliency =>
            {
                // Set the default timeout for all requests to 30 seconds
                var timeout = TimeSpan.FromSeconds(30);
                resiliency.AttemptTimeout.Timeout = timeout;
                resiliency.CircuitBreaker.SamplingDuration = timeout * 2;
                resiliency.TotalRequestTimeout.Timeout = timeout * 3;
            });

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });


        var app = builder.Build();

        // All health checks must pass for app to be
        // considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag
        // must pass for app to be considered alive
        app.MapHealthChecks("/alive", new()
        {
            Predicate = static r => r.Tags.Contains("live")
        });


        app.UseSystemWebAdapters();

        app.Map("/Core", (HttpContext context) =>
        {
            var session = context.AsSystemWeb().Session!;

            if (session["CoreCount"] is int count)
            {
                session["CoreCount"] = count + 1;
            }
            else
            {
                session["CoreCount"] = 0;
            }

            return session.Cast<string>().Select(key => new { Key = key, Value = session[key] });
        }).RequireSystemWebAdapterSession();

        app.MapRemoteApp();

        app.Run();
    }
}
