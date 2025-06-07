using C3D.Extensions.DependencyInjection.SystemWeb;
using EF6WebApp.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Collections.Generic;
using System.Data.Common;
using EF6WebApp.Models;
using System.Data.Entity.Infrastructure;
using System;
using System.Data.Entity.SqlServer;
using C3D.Extensions.DependencyInjection;
using System.Diagnostics;

[assembly: System.Web.PreApplicationStartMethod(typeof(EF6WebApp.MvcApplication), 
    nameof(EF6WebApp.MvcApplication.CheckDebugger))]

namespace EF6WebApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private ServiceProvider _serviceProvider;
        private static string dataDir;

        public static void CheckDebugger()
        {
            // This env variable is passed from Aspire as we don't have a clean way to attach the debugger to IIS Express
            // Use this if you don't enable the vsjitdebugger.exe hook
            if (System.Environment.GetEnvironmentVariable("Launch_Debugger_On_Start") == "true")
            {
                Debugger.Launch();
            }
        }

        protected void Application_Start()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var connectionStrings = System.Configuration.ConfigurationManager.ConnectionStrings;
            var initialConnectionStrings = new Dictionary<string, string>();
            for (var i = 0; i < connectionStrings.Count; i++)
            {
                var connectionString = connectionStrings[i];
                initialConnectionStrings["ConnectionStrings:" + connectionString.Name] = connectionString.ConnectionString;
            }
            configurationBuilder
                // Add the default connection string from web.config here first
                .AddInMemoryCollection(initialConnectionStrings)
                // Then use environment variables to override e.g. from Aspire
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection();

            services.AddSingleton<IConfiguration>(configuration);

            dataDir = Context.Server.MapPath("~/App_Data/Database");
            System.IO.Directory.CreateDirectory(dataDir);

            services
                .AddSingleton<MicrosoftSqlConnectionFactory>()
                .AddTransient(sp =>
                {
                    var connection = sp.GetRequiredKeyedService<DbConnection>("BloggingContext");
                    var context = ActivatorUtilities.CreateInstance<BlogContext>(sp, connection, true);
                    return context;
                })
                .AddKeyedTransient("BloggingContext", static (sp, key) =>
                {
                    var factory = sp.GetRequiredService<MicrosoftSqlConnectionFactory>();
                    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString((string)key);
                    var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                    // We can manipulate the connection string here as needed
                    if (connectionStringBuilder.DataSource== "(localdb)\\mssqllocaldb")
                    {

                        connectionStringBuilder.AttachDBFilename = dataDir + "EF6WebApp.mdf";
                    }

                    var connection = factory.CreateConnection(connectionStringBuilder.ToString());
                    return connection;
                });

            services.AddLogging(logging =>
            {
                logging.Configure(options =>
                {
                    options.ActivityTrackingOptions =
                        ActivityTrackingOptions.SpanId |
                        ActivityTrackingOptions.TraceId |
                        ActivityTrackingOptions.ParentId;
                });
                logging.AddOpenTelemetry(ot =>
                {
                    ot.IncludeFormattedMessage = true;
                    ot.IncludeScopes= true;
                    ot.ParseStateValues = true;
                });
            });

            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSqlClientInstrumentation(
                            options => options.SetDbStatementForText = 
                                (configuration.GetValue<string>("Environment") == "Development")
                        )
                        ;
                })
                .WithMetrics(metrics=>
                {
                    metrics
                        .AddAspNetInstrumentation()
                        .AddAspNetInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddSqlClientInstrumentation()
                        ;
                })
                .WithLogging(logging=>
                {

                })
            .UseOtlpExporter();

            services.AddTransient<BlogController>();

            _serviceProvider = services.BuildServiceProvider();

            System.Web.HttpRuntime.WebObjectActivator = new WebObjectActivator(_serviceProvider);
            System.Web.Mvc.DependencyResolver.SetResolver(new ServiceProviderDependencyResolver(_serviceProvider));

            var meterProvider = _serviceProvider.GetService<MeterProvider>();
            var tracerProvider = _serviceProvider.GetService<TracerProvider>();
            var loggerProvider = _serviceProvider.GetService<LoggerProvider>();

            System.Data.Entity.DbConfiguration.SetConfiguration(new MicrosoftSqlDbConfiguration());

            InitializeBlogContext(_serviceProvider);

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private static void InitializeBlogContext(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<MvcApplication>>();
            using var scope = services.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<BlogContext>();
            logger.LogInformation("BlogContext Connection String: {connectionString}", db.Database.Connection.ConnectionString);
            db.Database.CreateIfNotExists();
            db.Blogs.Add(new Blog { Name = $"Another Blog {DateTime.UtcNow:O}" });
            db.SaveChanges();
        }

        protected void Application_End()
        {
            _serviceProvider?.Dispose();
        }
    }
}
