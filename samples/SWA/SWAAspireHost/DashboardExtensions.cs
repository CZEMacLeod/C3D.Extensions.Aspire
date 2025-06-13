namespace AspireAppHostSWA;

//This was copied from https://github.com/davidfowl/aspire-ai-chat-demo/blob/main/AIChat.AppHost/DashboardExtensions.cs
public static class DashboardExtensions
{
    public static void AddDashboard(this IDistributedApplicationBuilder builder)
    {
        if (builder.ExecutionContext.IsPublishMode)
        {
            // The name aspire-dashboard is special cased and excluded from the default
            var dashboard = builder.AddContainer("dashboard", "mcr.microsoft.com/dotnet/nightly/aspire-dashboard", "9.3.0-amd64")
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
                        string protocol = "grpc";
                        if (r.TryGetLastAnnotation<OtlpProtocolAnnotation>(out var protocolAnnotation))
                        {
                            protocol = protocolAnnotation.Protocol;
                        }
                        switch (protocol)
                        {
                            case "http/protobuf":
                                // IIS Express resources use the http endpoint for telemetry
                                c.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = dashboard.GetEndpoint("http");
                                break;
                            default:
                                // Set the OTLP endpoint to the dashboard's OTLP endpoint
                                c.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = dashboard.GetEndpoint("otlp");
                                break;
                        }
                        c.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = protocol;
                        c.EnvironmentVariables["OTEL_SERVICE_NAME"] = r.Name;
                    });
                }

                return Task.CompletedTask;
            });
        }
    }

    public static IResourceBuilder<T> WithOtlpProtocol<T>(this IResourceBuilder<T> builder, string protocol) where T : IResource
    {
        ArgumentNullException.ThrowIfNullOrEmpty(protocol);

        return builder.WithAnnotation(new OtlpProtocolAnnotation(protocol), ResourceAnnotationMutationBehavior.Replace);
    }

    private class OtlpProtocolAnnotation : IResourceAnnotation
    {
        public OtlpProtocolAnnotation(string protocol) => Protocol = protocol;

        public string Protocol { get; }
    }
}