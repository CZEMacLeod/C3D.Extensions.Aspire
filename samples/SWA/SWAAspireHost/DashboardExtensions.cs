using C3D.Extensions.Aspire.IISExpress.Resources;

namespace AspireAppHostSWA;

//This was copied from https://github.com/davidfowl/aspire-ai-chat-demo/blob/main/AIChat.AppHost/DashboardExtensions.cs
public static class DashboardExtensions
{
    public static void AddDashboard(this IDistributedApplicationBuilder builder)
    {
        if (builder.ExecutionContext.IsPublishMode)
        {
            // The name aspire-dashboard is special cased and excluded from the default
            var dashboard = builder.AddContainer("dashboard", "mcr.microsoft.com/dotnet/nightly/aspire-dashboard")
                   .WithHttpEndpoint(targetPort: 18888)
                   .WithHttpEndpoint(name: "otlp", targetPort: 18889)
                   .PublishAsDockerComposeService((_, service) =>
                   {
                       service.Restart = "always";
                   });

            builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
            {
                // We loop over all resources and set the OTLP endpoint to the dashboard
                // we should make WithOtlpExporter() add an annotation so we can detect this
                // automatically in the future.
                foreach (var r in e.Model.Resources.OfType<IResourceWithEnvironment>())
                {
                    if (r == dashboard.Resource)
                    {
                        continue;
                    }

                    builder.CreateResourceBuilder(r).WithEnvironment(c =>
                    {
                        switch (r)
                        {
                            case IISExpressProjectResource or
                                 IISExpressSiteResource:
                                // IIS Express resources use the http endpoint for telemetry
                                c.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = dashboard.GetEndpoint("http");
                                c.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf";
                                break;
                            default:
                                // Set the OTLP endpoint to the dashboard's OTLP endpoint
                                c.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = dashboard.GetEndpoint("otlp");
                                c.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
                                break;
                        }
                        c.EnvironmentVariables["OTEL_SERVICE_NAME"] = r.Name;
                    });
                }

                return Task.CompletedTask;
            });
        }
    }
}