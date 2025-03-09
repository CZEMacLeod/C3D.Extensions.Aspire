# C3D.Extensions.Aspire

A set of packages to make it easier to work with IIS Express / System.Web projects, and extend debugging in Aspire.

# AppHost Packages

## [C3D.Extensions.Aspire.IISExpress](src/C3D/Extensions/Aspire/IISExpress/README.md)
## [C3D.Extensions.Aspire.VisualStudioDebug](src/C3D/Extensions/Aspire/VisualStudioDebug/README.md)

# Support Packages

## [C3D.Extensions.VisualStudioDebug](src/C3D/Extensions/VisualStudioDebug/README.md)

# Client Packages

## [C3D.Extensions.SystemWeb.OpenTelemetry.Application](src/C3D/Extensions/SystemWeb/OpenTelemetry/Application/README.md)

# Samples

## Simple
A basic project based on [MSBuild.SDK.SystemWeb](https://github.com/CZEMacLeod/MSBuild.SDK.SystemWeb)
which shows a very basic MVC5 application using the new SDK style, and can be launched by Aspire.
It supports OpenTelemetry and will correctly connect to the Aspire Dashboard.
Note that being IIS (Express) based, it does not start until the first web request, so you will need to launch it using the link on the dashboard.

## EF6
A more advanced project which uses Microsoft.Data.SqlClient to allow connections to any SQL Server using the latest technologies.
This has some code to allow connection strings to be copied from web.config to a Microsoft.Extensions.Configuration based version,
and creates a DependencyInjection container based on Microsoft.Extensions.DependencyInjection.
There are custom implementations of IServiceProvider for WebObjectActivator and IDependencyResolver for MVC5.
These allow you to inject ILogger and DbContext to a controller, filter etc.
This is a nieve implementation that does not deal with request scopes - all objects are either Singleton or Transient.
A localdb is created in the App_Data folder if you run the application directly and each time a new blog is created on startup.
If launched via aspire, it will wait for the sql server to start then start IISExpress. The database will be created afresh each time and there will only be a single blog entry.

## SWA
This shows using Aspire to handle [Incremental ASP.NET to ASP.NET Core Migration](https://learn.microsoft.com/aspnet/core/migration/inc/overview).
This consists of a Full Framework example app using MVC, with some of the code from the RemoteSession example added in.
The Core app is lightweight and only contains YARP, a Session variable route (per the RemoteSession example), OTLP telemetry, and HeathChecks.
Apire sets up a randomized app key used by both applications, and wires up the Urls for YARP etc.
Traces show the request span of the core app, the proxy request, and the framework processing.
