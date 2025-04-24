using EnvDTE;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Process = System.Diagnostics.Process;

namespace C3D.Extensions.VisualStudioDebug;

public class VisualStudioInstances : IDisposable
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<VisualStudioInstances> logger;
    private readonly ConcurrentDictionary<int, WeakReference<VisualStudioInstance>?> visualStudioInstances = new();
    private bool disposedValue;

    private void RefreshVisualStudioInstances()
    {
        var discards = new List<int>();
        var processes = new List<int>();
        foreach (var instance in visualStudioInstances)
        {
            if (instance.Value?.TryGetTarget(out var _) ?? false)
            {
                processes.Add(instance.Key);
            }
            else
            {
                discards.Add(instance.Key);
            }
        }
        discards.ForEach(d => visualStudioInstances.TryRemove(d, out var _));

        IEnumerable<Process> visualStudios = GetVisualStudioProcesses();
        foreach (var p in visualStudios)
        {
            if (!visualStudioInstances.ContainsKey(p.Id))
            {
                visualStudioInstances.TryAdd(p.Id, null);
            }
        }
    }
    private static IEnumerable<Process> GetVisualStudioProcesses() =>
        Process.GetProcesses()
            .Where(o => o.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase));

    public IEnumerable<VisualStudioInstance> GetVisualStudioInstances()
    {
        //var logger = serviceProvider.GetRequiredService<ILogger<VisualStudioInstance>>();

        RefreshVisualStudioInstances();
        foreach (var kvp in visualStudioInstances)
        {
            VisualStudioInstance? i = null;
            if (kvp.Value is null || kvp.Value.TryGetTarget(out i))
            {
                if (VisualStudioAttacher.TryGetVsInstance(kvp.Key, out _DTE? visualStudioInstance))
                {
                    i = new VisualStudioInstance(
                            Process.GetProcessById(kvp.Key),
                            visualStudioInstance,
                            serviceProvider.GetRequiredService<ILogger<VisualStudioInstance>>());
                    visualStudioInstances[kvp.Key] = new WeakReference<VisualStudioInstance>(i);
                }
                else
                {
                    logger.LogWarning("Failed to get VS Instance {id}", kvp.Key);
                }
                if (i is not null)
                {
                    yield return i;
                }
            }
        }
    }

    public VisualStudioInstances(IServiceProvider serviceProvider, ILogger<VisualStudioInstances> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task<VisualStudioInstance?> GetAttachedVisualStudioAsync(Process applicationProcess)
    {
        foreach (var vs in GetVisualStudioInstances())
        {
            foreach (var debug in await vs.GetDebuggedProcessesAsync())
            {
                if (debug.transport == WellKnown.Transports.Default && 
                    debug.id == applicationProcess.Id)
                {
                    return vs;
                }
            }
        }
        return null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                while(!visualStudioInstances.IsEmpty)
                {
                    var kvp = visualStudioInstances.First();
                    if (kvp.Value?.TryGetTarget(out var t) ?? false)
                    {
                        t.Dispose();
                    }
                    visualStudioInstances.TryRemove(kvp.Key, out var _);
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~VisualStudioInstances()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}


