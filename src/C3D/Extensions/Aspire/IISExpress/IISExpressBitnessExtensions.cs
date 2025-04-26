namespace C3D.Extensions.Aspire.IISExpress;

public static class IISExpressBitnessExtensions
{
    public static string GetIISExpressPath(this IISExpressBitness bitness) =>
        Path.Combine(bitness switch
        {
            IISExpressBitness.IISExpress32Bit => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            IISExpressBitness.IISExpress64Bit => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            _ => throw new ArgumentOutOfRangeException(nameof(bitness), bitness, null)
        }, "IIS Express");

    public static (IISExpressBitness bitness, string dirPath) GetIISExpressPath(this IISExpressBitness? bitness)
    {
        var bitnessToUse = bitness ?? Annotations.IISExpressBitnessAnnotation.DefaultBitness;
        return (bitnessToUse, bitnessToUse.GetIISExpressPath());
    }

    public static (IISExpressBitness bitness, string exePath) GetIISExpressExe(this IISExpressBitness? bitness)
    {
        var (bitnessToUse, path) = bitness.GetIISExpressPath();
        return (bitnessToUse, Path.Combine(path, "iisexpress.exe"));
    }
}