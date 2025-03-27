﻿using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using DTEProcess = EnvDTE90.Process3;
using Process = System.Diagnostics.Process;

namespace C3D.Extensions.VisualStudioDebug;

public class VisualStudioInstance : IDisposable
{
    private readonly Process process;
    private DTE dte;
    private readonly ILogger<VisualStudioInstance> logger;
    private bool disposedValue;

    internal VisualStudioInstance(System.Diagnostics.Process process, DTE vs, ILogger<VisualStudioInstance> logger)
    {
        this.process = process;
        dte = vs;
        this.logger = logger;
    }

    public Process Process => process;
    public int Id => process.Id;
    public string ProcessName => process.ProcessName;


    public string? GetSolution()
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

    public IEnumerable<(int id, string transport, string name)> GetDebuggedProcesses()
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

    public string GetDebugTransportName(string transport)
    {
        var result = dte.GetDebugTransport(transport);
        return result.transport.Name;
    }

    public IEnumerable<(string id, string name)> GetDebugTransports()
    {
        var debugger = dte.GetDebugger();
        foreach (EnvDTE80.Transport e in debugger.Transports)
        {
            yield return (e.ID, e.Name);
        }
    }

    public IEnumerable<(int id, string name, bool isDebugged)> GetDebugProcesses(string transport, string? qualifier)
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

    public IEnumerable<(string id, string name, int result)> GetDebugEngines(string transport = "default")
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
    public void AttachVisualStudioToProcess(int processId, params string[] engines)
        => AttachVisualStudioToProcess(vs => (vs.GetDebugTransport("default").transport,
                        vs.Debugger.LocalProcesses
                        .Cast<DTEProcess>()
                        .FirstOrDefault(process => process.ProcessID == processId)),
                engines);

    /// <summary>
    /// The method to use to attach visual studio to a process by specifying connection information.
    /// </summary>
    /// <param name="applicationProcess">
    /// The application process that needs to be debugged.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the application process is null.
    /// </exception>
    public void AttachVisualStudioToProcess(string transport, string? qualifier, params string[] engines)
        => AttachVisualStudioToProcess(vs =>
            {
                var (debugger, port) = vs.GetDebugTransport(transport);
                Processes processes = debugger.GetProcesses(port, qualifier ?? string.Empty);
                if (processes.Count != 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(qualifier), "Qualifer did not result in a single process");
                }
                return (port, (DTEProcess)processes.Item(1));
            },
            engines);


    private void AttachVisualStudioToProcess(Func<DTE, (Transport transport, DTEProcess? process)> applicationProcess, params string[] engines)
    {
        // Register the message filter scoped to this method
        using var filter = new MessageFilter(logger);

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


    // Based on: https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-studio-2010/ms228772(v=vs.100)?redirectedfrom=MSDN#example
    private class MessageFilter : IMessageFilter, IDisposable
    {
        //
        // Class containing the IMessageFilter
        // thread error-handling functions.

        private readonly IMessageFilter? originalFilter;
        private readonly ILogger logger;

        public MessageFilter(ILogger logger)
        {
            IMessageFilter? newFilter = this;
            _ = CoRegisterMessageFilter(newFilter, out IMessageFilter? oldFilter);
            this.originalFilter = oldFilter;
            this.logger = logger;
        }

        //
        // IOleMessageFilter functions.
        // Handle incoming thread requests.
        int IMessageFilter.HandleInComingCall(int dwCallType,
            System.IntPtr hTaskCaller, int dwTickCount, System.IntPtr
            lpInterfaceInfo)
        {
            //Return the flag SERVERCALL_ISHANDLED.
            return 0;
        }

        // Thread call was rejected, so try again.
        int IMessageFilter.RetryRejectedCall(System.IntPtr
            hTaskCallee, int dwTickCount, int dwRejectType)
        {
            logger.LogWarning("RetryRejectedCall {callee} {tickCount} {rejectType}", hTaskCallee, dwTickCount, dwRejectType);
            if (dwRejectType == 2)
            // flag = SERVERCALL_RETRYLATER.
            {
                // Retry the thread call immediately if return >=0 & 
                // <100.
                return 99;
            }
            // Too busy; cancel call.
            return -1;
        }

        int IMessageFilter.MessagePending(System.IntPtr hTaskCallee,
            int dwTickCount, int dwPendingType)
        {
            logger.LogInformation("MessagePending {callee} {tickCount} {pendingType}", hTaskCallee, dwTickCount, dwPendingType);
            //Return the flag PENDINGMSG_WAITDEFPROCESS.
            return 2;
        }

        // Implement the IOleMessageFilter interface.
        [DllImport("Ole32.dll")]
        private static extern int
            CoRegisterMessageFilter(IMessageFilter? newFilter, out
            IMessageFilter? oldFilter);

        public void Dispose()
        {
            _ = CoRegisterMessageFilter(originalFilter, out IMessageFilter? _);
        }
    }

    //// Definition of the IMessageFilter interface which we need to implement and 
    //// register with the CoRegisterMessageFilter API.    
    [ComImport()]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMessageFilter     // Renamed to avoid confusion w/ System.Windows.Forms.IMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);
        [PreserveSig]
        int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);
        [PreserveSig]
        int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
    };
}

