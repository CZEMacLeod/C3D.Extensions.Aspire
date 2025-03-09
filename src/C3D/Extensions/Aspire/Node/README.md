# C3D.Extensions.Aspire.Node

A way to reference and execute an Node based project (.eproj) using Aspire.
Connects to the instance of VisualStudio running the AprireHost and attaches the debugger to the IIS Express instance so that the project can be debugged as normal.
Adds a healthcheck to the Node resource to show whether the debugger has been attached.
A future option would be to send the initial request to spin up the site once the debugger is attached.

## Example
```cs
var webapp = builder.AddNodeApp<Projects.ExpressProject>("webapp")
    .WithHttpEndpoint(env: "PORT")
    .WithOtlpExporter()
    .WithWatch()
    .WithDebugger()
    .WithHttpHealthCheck("/alive");
```

## Options
- `AddNodeApp<T>` Adds a referenced project as a Node app. Uses the project directory to launch node.
- `WithWatch()` Injects the `--watch` option in the node arguments
- `WithDebugger()` Sets up the logic to attach the debugger using the V8 Inspection protocol and watch for the debugger connection string.

## Known Issues
- This doesn't work with npm at the moment, as the debugger options get confused between the npm runner and it's child processes.
