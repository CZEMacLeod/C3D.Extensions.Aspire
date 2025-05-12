# C3D.Extensions.Aspire.Fluent

## Key Functions
- Environment-based Configuration
  Easily apply different configurations depending on the hosting environment (e.g., Development, Staging, Production).
- 
- Operation Mode Detection
  Adjust host behavior based on the current operation mode, such as Run, Publish, or Inspect.
- 
- Test Environment Awareness
  Automatically detect if the host is running under test conditions (by checking for the presence of the `Aspire.Hosting.Testing` assembly in the call stack) and apply test-specific settings.

## Usage Example
Suppose you want to configure services differently for development and production:
```cs
        var sqldb = builder.WhenDevelopment(b => b.AddSqlServer("sql").AddDatabase("sqldb"));
        builder.AddProject<Projects.MyWebApp>("webapp")
            .WhenDevelopment(r => r.WithReference(sqldb!, "sqldb")
                                   .WaitFor(sqldb!),
                             r => r.WithEnvironment("ConnectionString_sqldb","data source=(localdb)\mssqllocaldb;initial catalog=webapp;integrated security=True;MultipleActiveResultSets=True;App=webapp"));
```

For most of the methods, there is an else case (which is optional), and extension methods for `IResourceBuilder` and `IDistributedApplicationBuilder`

You can also nest methods, e.g. `WhenUnderTest` inside the lambda of `WhenDevelopment`.

If you want to only use the 'else' case, you can simply return the resourceBuilder or null in the match function.
```cs
    var sqldb = builder.WhenProduction(_ => null, b => b.AddSqlServer("sql").AddDatabase("sqldb"));
    builder.AddProject<Projects.MyWebApp>("webapp")
        .WhenProduction(r => r,
                        r => r.WithReference(sqldb!, "sqldb")
                              .WaitFor(sqldb!));
```
In the case of UnitTesting, there are explicit `WhenNotUnderTest` methods which read more cleanly than `WhenUnderTest(r=>r, r=>r.... )`.

Note: For detailed API documentation and advanced usage, refer to the source code or inline XML comments within the project.