using EnvDTE;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace C3D.Extensions.VisualStudioDebug;

/// <summary>
/// Based on code from <a href="https://gist.github.com/3813175">this gist</a>.
/// </summary>
/// 
internal static class VisualStudioAttacher
{
    #region "ole32"

    [DllImport("ole32.dll")]
    internal static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll")]
    internal static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    #endregion

    internal static bool TryGetVsInstance(int processId, [NotNullWhen(true)] out _DTE? instance)
    {
        IntPtr numFetched = IntPtr.Zero;
        var monikers = new IMoniker[1];

        GetRunningObjectTable(0, out IRunningObjectTable runningObjectTable);
        runningObjectTable.EnumRunning(out IEnumMoniker monikerEnumerator);
        monikerEnumerator.Reset();

        while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
        {
            CreateBindCtx(0, out IBindCtx ctx);

            monikers[0].GetDisplayName(ctx, null, out string runningObjectName);

            runningObjectTable.GetObject(monikers[0], out object runningObjectVal);

            if (runningObjectVal is _DTE && runningObjectName.StartsWith("!VisualStudio"))
            {
                int currentProcessId = int.Parse(runningObjectName.Split(':')[1]);

                if (currentProcessId == processId)
                {
                    instance = (_DTE)runningObjectVal;
                    return true;
                }
            }
        }

        instance = null;
        return false;
    }
}


