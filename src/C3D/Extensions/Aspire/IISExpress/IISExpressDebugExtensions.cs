using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.IISExpress.Resources;
using C3D.Extensions.Aspire.VisualStudioDebug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class IISExpressDebugExtensions
{
    public static IDebugBuilder<IISExpressSiteResource> WithDebugger(this IResourceBuilder<IISExpressSiteResource> resourceBuilder, DebugMode debugMode = DebugMode.VisualStudio)
        => DebugResourceBuilderExtensions.WithDebugger(resourceBuilder, debugMode)
            .WithDebugEngine(C3D.Extensions.VisualStudioDebug.WellKnown.Engines.Net4)
            .WithDebuggerHealthcheck();

    public static IDebugBuilder<IISExpressProjectResource> WithDebugger(this IResourceBuilder<IISExpressProjectResource> resourceBuilder,
      DebugMode debugMode = DebugMode.VisualStudio) =>
        DebugResourceBuilderExtensions.WithDebugger(resourceBuilder, debugMode)
            .WithDebugEngine(C3D.Extensions.VisualStudioDebug.WellKnown.Engines.Net4)
            .WithDebuggerHealthcheck();
}
