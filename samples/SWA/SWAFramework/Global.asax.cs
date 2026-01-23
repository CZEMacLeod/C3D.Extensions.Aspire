using System;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using C3D.Extensions.SystemWeb.OpenTelemetry.Application;
using Microsoft.AspNetCore.SystemWebAdapters.Hosting;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

[assembly: PreApplicationStartMethod(typeof(SWAFramework.MvcApplication), nameof(SWAFramework.MvcApplication.WaitForDebugger))]

namespace SWAFramework;

public class MvcApplication : OpenTelemeteryApplication
{
    public static void WaitForDebugger()
    {
        if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["WaitForDebugger"])) return;

        var timeout = DateTime.UtcNow.AddSeconds(30);
        while (!System.Diagnostics.Debugger.IsAttached && DateTime.UtcNow<timeout)
        {
            System.Threading.Thread.Sleep(100);
        }
    }

    protected override void Application_Start()
    {
        base.Application_Start();

        HttpApplicationHost.RegisterHost(builder =>
        {
            builder
                .AddSystemWebAdapters()
                .AddProxySupport(options => options.UseForwardedHeaders = true)
                .AddSessionSerializer(options =>
                {
                })
                .AddJsonSessionSerializer(options =>
                {
                    options.RegisterKey<int>("CoreCount");
                })
                .AddRemoteAppServer(options => options.ApiKey = ConfigurationManager.AppSettings["RemoteApp:ApiKey"])
                .AddSessionServer(options =>
                {
                });

            builder.AddDataProtection();
            builder.AddWebObjectActivator();
        });

        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }

    protected void Application_PostAcquireRequestState(object sender, EventArgs e)
    {
        if (((HttpApplication)sender).Context.Session is { } session)
        {
            if (session["FrameworkCount"] is int count)
            {
                session["FrameworkCount"] = count + 1;
            }
            else
            {
                session["FrameworkCount"] = 0;
            }
        }
    }

    protected override void ConfigureResource(ResourceBuilder builder)
    {
        base.ConfigureResource(builder);
        builder.AddProcessRuntimeDetector();
        builder.AddAssemblyMetadataDetector();
    }
}

