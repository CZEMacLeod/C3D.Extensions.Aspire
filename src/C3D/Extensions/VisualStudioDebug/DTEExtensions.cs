using EnvDTE;
using EnvDTE80;
using System.Diagnostics;

namespace C3D.Extensions.VisualStudioDebug;

internal static class DTEExtensions
{

    internal static EnvDTE90.Debugger3 GetDebugger(this DTE visualStudioInstance)
    {
        var dte = (EnvDTE80.DTE2)visualStudioInstance;
        return (EnvDTE90.Debugger3)dte.Debugger;
    }

    internal static (EnvDTE90.Debugger3 debugger, Transport transport) GetDebugTransport(this DTE vs, string transport)
    {
        var debugger = vs.GetDebugger();
        Transport? port = null;
        if (Guid.TryParse(transport, out var _))
        {
            foreach (EnvDTE80.Transport e in debugger.Transports)
            {
                if (e.ID == transport)
                {
                    port = e;
                    break;
                }
            }
        }
        else
        {
            port = debugger.Transports.Item(transport);
            if (!string.Equals(port.Name,transport, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Transport {transport} does not match returned object {port.Name} {port.ID}");
                port = null;
                foreach (EnvDTE80.Transport e in debugger.Transports)
                {
                    Debug.WriteLine(e.Name);
                }
            }
        }
        if (port is null) throw new ArgumentOutOfRangeException(nameof(transport));
        return (debugger, port);
    }

    internal static IEnumerable<Engine> ResolveDebugEngines(this Transport transport, string[] engines)
    {
        if (engines.Length == 0)
        {
            foreach (Engine e in transport.Engines)
            {
                Debug.WriteLine($"Available Engine: {e.Name} {e.ID}");
            }
        }
        foreach (var engine in engines)
        {
            Engine resolvedEngine=null!;
            if (Guid.TryParse(engine, out var _))
            {
                foreach (Engine e in transport.Engines)
                {
                    if (e.ID == engine)
                    {
                        resolvedEngine=e;
                        break;
                    }
                }
            }
            else
            {
                resolvedEngine = transport.Engines.Item(engine);
            }
            if (resolvedEngine is null) throw new ArgumentOutOfRangeException(nameof(engines));
            yield return resolvedEngine;
        }
    }
}


