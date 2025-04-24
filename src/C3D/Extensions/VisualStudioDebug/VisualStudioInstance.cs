using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using DTEProcess = EnvDTE90.Process3;
using Process = System.Diagnostics.Process;

namespace C3D.Extensions.VisualStudioDebug;

public class VisualStudioInstance : IDisposable
{
    private readonly Process process;
    private DTE dte;
    private readonly ILogger<VisualStudioInstance> logger;
    private readonly STASingleThreadedScheduler sta;
    private bool disposedValue;

    internal VisualStudioInstance(System.Diagnostics.Process process, DTE vs, ILogger<VisualStudioInstance> logger)
    {
        this.process = process;
        dte = vs;
        this.logger = logger;
        this.sta = new STASingleThreadedScheduler(logger);
        staTaskFactory = new(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, sta);
    }

    private readonly TaskFactory staTaskFactory;

    public Process Process => process;
    public int Id => process.Id;
    public string ProcessName => process.ProcessName;

    private Task RunOnSTAScheduler(Func<Task> func) => staTaskFactory.StartNew(func).Unwrap();
    private Task<T> RunOnSTAScheduler<T>(Func<Task<T>> func) => staTaskFactory.StartNew(func).Unwrap();
    private Task RunOnSTAScheduler(Action func) => staTaskFactory.StartNew(func);
    private Task<T> RunOnSTAScheduler<T>(Func<T> func) => staTaskFactory.StartNew(func);

    public Task<string?> GetSolutionAsync() => RunOnSTAScheduler(GetSolution);

    private string? GetSolution()
    {
        try
        {
            return dte.Solution.FullName;
        }
        catch (Exception)
        {
        }
        return null;
    }

    public Task<List<(int id, string transport, string name)>> GetDebuggedProcessesAsync() => RunOnSTAScheduler(() => GetDebuggedProcesses().ToList());
    private IEnumerable<(int id, string transport, string name)> GetDebuggedProcesses()
    {
        foreach (EnvDTE.Process debuggedProcess in dte.Debugger.DebuggedProcesses)
        {
            if (debuggedProcess is DTEProcess p3)
            {
                yield return (p3.ProcessID, p3.Transport.ID, p3.Name);
            }
            else
            {
                yield return (debuggedProcess.ProcessID, WellKnown.Transports.Default, debuggedProcess.Name);
            }
        }
    }

    public Task<string> GetDebugTransportNameAsync(string transport) => RunOnSTAScheduler(() => GetDebugTransportName(transport));
    private string GetDebugTransportName(string transport)
    {
        var result = dte.GetDebugTransport(transport);
        return result.transport.Name;
    }

    public Task<List<(string id, string name)>> GetDebugTransportsAsync() => RunOnSTAScheduler(() => GetDebugTransports().ToList());
    private IEnumerable<(string id, string name)> GetDebugTransports()
    {
        var debugger = dte.GetDebugger();
        foreach (EnvDTE80.Transport e in debugger.Transports)
        {
            yield return (e.ID, e.Name);
        }
    }

    public Task<List<(int id, string name, bool isDebugged)>> GetDebugProcessesAsync(string transport, string? qualifier) => RunOnSTAScheduler(() => GetDebugProcesses(transport, qualifier).ToList());
    private IEnumerable<(int id, string name, bool isDebugged)> GetDebugProcesses(string transport, string? qualifier)
    {
        (var debugger, var port) = dte.GetDebugTransport(transport);
        if (port is null)
            yield break;

        Processes processes = debugger.GetProcesses(port, qualifier ?? string.Empty);
        foreach (EnvDTE80.Process2 e in processes)
        {
            yield return (e.ProcessID, e.Name, e.IsBeingDebugged);
        }
    }

    public Task<List<(string id, string name, int result)>> GetDebugEnginesAsync(string transport) => RunOnSTAScheduler(() => GetDebugEngines(transport).ToList());
    private IEnumerable<(string id, string name, int result)> GetDebugEngines(string transport = "default")
    {
        (_, var port) = dte.GetDebugTransport(transport);
        if (port is null)
            yield break;

        foreach (EnvDTE80.Engine e in port.Engines)
        {
            yield return (e.ID, e.Name, e.AttachResult);
        }
    }


    /// <summary>
    /// The method to use to attach visual studio to a specified process.
    /// </summary>
    /// <param name="applicationProcess">
    /// The application process that needs to be debugged.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the application process is null.
    /// </exception>
    public Task AttachVisualStudioToProcessAsync(int processId, params string[] engines)
        => RunOnSTAScheduler(() => AttachVisualStudioToProcess(vs => (vs.GetDebugTransport("default").transport,
                        vs.Debugger.LocalProcesses
                        .Cast<DTEProcess>()
                        .FirstOrDefault(process => process.ProcessID == processId)),
                engines));

    /// <summary>
    /// The method to use to attach visual studio to a process by specifying connection information.
    /// </summary>
    /// <param name="applicationProcess">
    /// The application process that needs to be debugged.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the application process is null.
    /// </exception>
    public Task AttachVisualStudioToProcessAsync(string transport, string? qualifier, params string[] engines)
        => RunOnSTAScheduler(() => AttachVisualStudioToProcess(vs =>
            {
                var (debugger, port) = vs.GetDebugTransport(transport);
                Processes processes = debugger.GetProcesses(port, qualifier ?? string.Empty);
                if (processes.Count != 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(qualifier), "Qualifer did not result in a single process");
                }
                return (port, (DTEProcess)processes.Item(1));
            },
            engines));

    private void AttachVisualStudioToProcess(Func<DTE, (Transport transport, DTEProcess? process)> applicationProcess, params string[] engines)
    {
        // Find the process you want the VS instance to attach to...
        var (transport, process) = applicationProcess(dte);

        // Attach to the process.
        if (process != null)
        {
            Engine[] resolvedEngines;
            try
            {
                resolvedEngines = transport.ResolveDebugEngines(engines).ToArray();
            }
            catch (Exception e)
            {
                logger.LogError("Failed to resolve engines {engines}", engines);
                throw new ArgumentException("Failed to resolve engines", nameof(engines), e);
            }
            if (resolvedEngines.Length == 0)
            {
                process.Attach();
            }
            else
            {
                process.Attach2(resolvedEngines);
            }
        }
        else
        {
            throw new InvalidOperationException("Visual Studio cannot find specified application");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                process.Dispose();
                sta.Dispose();
            }

            dte = null!;

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~VisualStudioInstance()
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