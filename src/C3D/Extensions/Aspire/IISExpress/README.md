# C3D.Extensions.Aspire.IISExpress

A way to reference and execute an IIS Express based project (ASP.NET 4.x) using Aspire.
Connects to the instance of VisualStudio running the AprireHost and attaches the debugger to the IIS Express instance so that the project can be debugged as normal.
Adds a healthcheck to the IIS Express resource to show whether the debugger has been attached.
A future option would be to send the initial request to spin up the site once the debugger is attached.

## Breaking Changes
From version 0.3 onwards, the SystemWebAdapters extensions are no longer included in this package. 
If you were using the SystemWebAdapters extensions, you will need to add a reference to the `C3D.Extensions.Aspire.SystemWebAdapters` package instead.

## Known Issues
- The `$(SolutionDir)\.vs\$(SolutionName)\config\applicationhost.config` file is not normally checked in as part of the source. It will be recreated with default properties from the aspire configuration.
If you need to run the web application manually, the visual studio may create it for you.
You can control how this is handled by using the `WithTemporaryConfig()` and `WithDefaultIISExpressEndpoints()` methods.
See the SWA sample for an example of how to use these methods.
- There is no 'easy' way to automatically start up the IIS Express based website - SystemWeb applications start-up on their first web request. You can add a healthcheck to trigger this.