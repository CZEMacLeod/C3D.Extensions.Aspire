using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace C3D.Extensions.VisualStudioDebug;

internal sealed class STASingleThreadedScheduler : TaskScheduler, IDisposable
{
    private readonly BlockingCollection<Task> _tasks = new();
    private readonly Thread _thread;

    public STASingleThreadedScheduler(ILogger logger)
    {
        _thread = new Thread(() =>
        {
            using var messageFilter = new MessageFilter(logger);
            foreach (var task in _tasks.GetConsumingEnumerable())
            {
                TryExecuteTask(task);
            }
        })
        {
            IsBackground = true
        };

        _thread.SetApartmentState(ApartmentState.STA);

        _thread.Start();
    }

    protected override IEnumerable<Task>? GetScheduledTasks() => _tasks.ToArray();

    protected override void QueueTask(Task task) => _tasks.Add(task);

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // We want to force it to run in the thread we created, so we return false here.
        return false;
    }

    public void Dispose()
    {
        _tasks.CompleteAdding();
        _thread.Join();
        _tasks.Dispose();
    }

    public override int MaximumConcurrencyLevel => 1;


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
            logger.LogTrace("RetryRejectedCall {callee} {tickCount} {rejectType}", hTaskCallee, dwTickCount, dwRejectType);
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
            logger.LogTrace("MessagePending {callee} {tickCount} {pendingType}", hTaskCallee, dwTickCount, dwPendingType);
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