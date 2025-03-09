# C3D.Extensions.Aspire

[![Build Status](https://dev.azure.com/flexviews/OSS.Build/_apis/build/status%2FCZEMacLeod.C3D.Extensions.Aspire?branchName=main)](https://dev.azure.com/flexviews/OSS.Build/_build/latest?definitionId=88&branchName=main)
[![Build and Run IIS Express Tests](https://github.com/CZEMacLeod/C3D.Extensions.Aspire/actions/workflows/dotnet-msbuild-vstest.yml/badge.svg)](https://github.com/CZEMacLeod/C3D.Extensions.Aspire/actions/workflows/dotnet-msbuild-vstest.yml)

A set of packages to make it easier to work with Aspire.
Specific packages enable working with IIS Express / System.Web projects.
Others extend debugging in Aspire.
Please see the individual package readmes for more information.

# AppHost Packages

## [C3D.Extensions.Aspire.IISExpress](src/C3D/Extensions/Aspire/IISExpress/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Aspire.IISExpress.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.IISExpress)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Aspire.IISExpress.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.IISExpress)

## [C3D.Extensions.Aspire.VisualStudioDebug](src/C3D/Extensions/Aspire/VisualStudioDebug/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Aspire.VisualStudioDebug.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.VisualStudioDebug)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Aspire.VisualStudioDebug.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.VisualStudioDebug)

## [C3D.Extensions.Aspire.OutputWatcher](src/C3D/Extensions/Aspire/OutputWatcher/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Aspire.OutputWatcher.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.OutputWatcher)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Aspire.OutputWatcher.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.OutputWatcher)

## [C3D.Extensions.Aspire.WaitForOutput](src/C3D/Extensions/Aspire/WaitForOutput/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Aspire.WaitForOutput.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.WaitForOutput)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Aspire.WaitForOutput.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.WaitForOutput)

## [C3D.Extensions.Aspire.Fluent](src/C3D/Extensions/Aspire/Fluent/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Aspire.Fluent.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.Fluent)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Aspire.Fluent.svg)](https://nuget.org/packages/C3D.Extensions.Aspire.Fluent)

## [C3D.Extensions.Aspire.SystemWebAdapters](src/C3D/Extensions/Aspire/SystemWebAdapters/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.Aspire.SystemWebAdapters.svg)](https://nuget.org/packages/C3D.Extensions.SystemWebAdapters.Fluent)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.Aspire.SystemWebAdapters.svg)](https://nuget.org/packages/C3D.Extensions.SystemWebAdapters.Fluent)

> [!WARNING]  
> I am not a node developer and the support is currently pretty basic. 
> This is mainly to test and demonstrate using the `VisualStudioDebug` packages to connect with alternate transports and debug engines.

# Support Packages

## [C3D.Extensions.VisualStudioDebug](src/C3D/Extensions/VisualStudioDebug/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.VisualStudioDebug.svg)](https://nuget.org/packages/C3D.Extensions.VisualStudioDebug)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.VisualStudioDebug.svg)](https://nuget.org/packages/C3D.Extensions.VisualStudioDebug)

# Client Packages

## [C3D.Extensions.SystemWeb.OpenTelemetry.Application](src/C3D/Extensions/SystemWeb/OpenTelemetry/Application/README.md)
[![NuGet package](https://img.shields.io/nuget/v/C3D.Extensions.SystemWeb.OpenTelemetry.Application.svg)](https://nuget.org/packages/C3D.Extensions.SystemWeb.OpenTelemetry.Application)
[![NuGet downloads](https://img.shields.io/nuget/dt/C3D.Extensions.SystemWeb.OpenTelemetry.Application.svg)](https://nuget.org/packages/C3D.Extensions.SystemWeb.OpenTelemetry.Application)

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

## Node
This shows using Aspire to develop a simple Node / Express based application with full debugging support while using Aspire.
Simply set a breakpoint in `app.js` such as on line 62 - the delegate body.
```js
app.get('/', function (req, res) {
    res.send('Hello World!');
});
```
Set the Startup Project to `AspireAppHostNodeJS` and start the project.

> [!WARNING]  
> I am not a node developer and the example application is hacked together from examples from the [opentelemetry](https://github.com/open-telemetry/opentelemetry-js) and [aspire-samples](https://github.com/dotnet/aspire-samples) repositories.

## WaitForConsole
This shows how to use the WaitForOutput package to wait for a console app to output a specific message before starting another process.
It also shows how to monitor for a string using a regex, and use the matched string as an environment variable in the next process.