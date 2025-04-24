using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

public static class NodeHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// </summary>
    public static IResourceBuilder<NodeAppResource> RunWithHttpsDevCertificate(this IResourceBuilder<NodeAppResource> builder, string certFileEnv, string certKeyFileEnv, string httpPortEnv)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            DevCertHostingExtensions.RunWithHttpsDevCertificate(builder, certFileEnv, certKeyFileEnv, (certFilePath, certKeyPath) =>
            {
                builder.WithEnvironment(context =>
                {
                    // Configure Node to trust the ASP.NET Core HTTPS development certificate as a root CA.
                    if (context.EnvironmentVariables.TryGetValue(certFileEnv, out var certPath))
                    {
                        context.EnvironmentVariables["NODE_EXTRA_CA_CERTS"] = certPath;
                    }
                });
            });
        }

        return builder;
    }

    public static IResourceBuilder<NodeAppResource> WithHttpsRedirctionPort(this IResourceBuilder<NodeAppResource> builder,
        string env, string endpointName = "https") => WithHttpsRedirctionPort(builder, env, builder.GetEndpoint(endpointName));

    private static IResourceBuilder<NodeAppResource> WithHttpsRedirctionPort(this IResourceBuilder<NodeAppResource> builder, string env, EndpointReference httpsEndpoint) => 
        builder.WithEnvironment(context => 
            context.EnvironmentVariables[env] = ReferenceExpression.Create($"{httpsEndpoint.Property(EndpointProperty.Port)}"));
}