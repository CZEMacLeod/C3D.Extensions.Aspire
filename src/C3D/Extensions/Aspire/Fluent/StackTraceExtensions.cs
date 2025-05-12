using System.Diagnostics;

namespace C3D.Extensions.Aspire.Fluent;

public static class StackTraceExtensions
{
    private static StackFrame[] GetStackFrames(this StackTrace callStack) => callStack.GetFrames() ?? [];

    public static bool ContainsAssembly(this StackTrace callStack, string assemblyName) =>
        callStack.GetStackFrames().Any(sf =>
            sf.GetMethod()?.DeclaringType?.Assembly.GetName().Name == assemblyName);

    public static bool ContainsAspireTesting(this StackTrace callStack) => callStack.ContainsAssembly("Aspire.Hosting.Testing");
}
