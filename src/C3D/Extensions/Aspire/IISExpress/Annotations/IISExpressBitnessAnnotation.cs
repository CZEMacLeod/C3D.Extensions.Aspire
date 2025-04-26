using Aspire.Hosting.ApplicationModel;

namespace C3D.Extensions.Aspire.IISExpress.Annotations;

public class IISExpressBitnessAnnotation : IResourceAnnotation
{
    private readonly IISExpressBitness bitness;

    internal IISExpressBitnessAnnotation(IISExpressBitness bitness) => this.bitness = bitness;

    public static IISExpressBitness DefaultBitness => Environment.Is64BitOperatingSystem ? 
        IISExpressBitness.IISExpress64Bit : IISExpressBitness.IISExpress32Bit;

    public IISExpressBitness Bitness => bitness;
}
