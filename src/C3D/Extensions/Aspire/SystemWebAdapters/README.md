# C3D.Extensions.Aspire.SystemWebAdapters

Extension methods to allow for easy configuration in Aspire of a [SystemWeb-Adapters](https://github.com/dotnet/systemweb-adapters) setup with an IISExpress based ASP.NET 4.x project and an ASP.NET Core based project.

Previously this was available directly in the [C3D.Extensions.Aspire.IISExpress](https://nuget.org/packages/C3D.Extensions.Aspire.IISExpress) package but has been moved to this package to separate the concerns.

## Example Usage
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <Sdk Name="Aspire.AppHost.Sdk" Version="9.2.0" />

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsAspireHost>true</IsAspireHost>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="9.2.0" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="C3D.Extensions.Aspire.SystemWebAdapters" Version="0.2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SWACore\SWACore.csproj" />
    <ProjectReference Include="..\SWAFramework\SWAFramework.csproj" />
  </ItemGroup>
</Project>
```

```csharp
using Aspire.Hosting;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions()
{
    Args = args,
    AllowUnsecuredTransport = true
});

var framework = builder
    .AddIISExpressProject<Projects.SWAFramework>("framework")
    .WithTemporaryConfig()
    .WithDefaultIISExpressEndpoints());

var core = builder.AddProject<Projects.SWACore>("core");

var swa = builder
    .AddSystemWebAdapters("swa")
    .WithFramework(framework)
    .WithCore(core);

builder.Build().Run();
```