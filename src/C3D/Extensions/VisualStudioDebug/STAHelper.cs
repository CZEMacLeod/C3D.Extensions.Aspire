namespace C3D.Extensions.VisualStudioDebug;

internal static class STAHelper
{
    public static void Run(Action action)
    {
        var thread = new Thread(() => action());
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
