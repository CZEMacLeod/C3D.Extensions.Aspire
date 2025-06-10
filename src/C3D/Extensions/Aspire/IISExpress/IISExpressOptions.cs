using Aspire.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace C3D.Extensions.Aspire.IISExpress;

public class IISExpressOptions : IValidatableObject
{
    private readonly Lazy<Assembly?> _assemblyLazy;
    private readonly Lazy<string?> _solutionNameLazy;
    private readonly Lazy<string?> _solutionDirLazy;
    private string? solutionName;
    private string? solutionDir;
    private Assembly? _assembly;

    /// <summary>
    /// The AssemblyName of the AppHost project for loading configuration attributes; if not set defaults to Assembly.GetEntryAssembly().
    /// </summary>
    public string? AssemblyName { get; set; }

    [Required]
    public string? SolutionName { get => solutionName ?? _solutionNameLazy.Value; set => solutionName = value; }

    [Required]
    public string? SolutionDir { get => solutionDir ?? _solutionDirLazy.Value; set => solutionDir = value; }

    public IISExpressOptions()
    {
        _assemblyLazy = new(ResolveAssembly);
        _solutionNameLazy = new(ResolveSolutionName);
        _solutionDirLazy = new(ResolveSolutionDir);
    }

    internal Assembly? Assembly
    {
        get => _assembly ?? _assemblyLazy.Value;
        set => _assembly = value;
    }

    private static string? GetMetadataValue(IEnumerable<AssemblyMetadataAttribute>? assemblyMetadata, string key)
    {
        return assemblyMetadata?.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private string? ResolveSolutionDir()
    {
        var assemblyMetadata = Assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        return GetMetadataValue(assemblyMetadata, "apphostsolutiondir");
    }

    private string? ResolveSolutionName()
    {
        var assemblyMetadata = Assembly?.GetCustomAttributes<AssemblyMetadataAttribute>();
        return GetMetadataValue(assemblyMetadata, "apphostsolutionname");
    }

    private Assembly? ResolveAssembly()
    {
        // Calculate DCP locations from configuration options
        var appHostAssembly = Assembly.GetEntryAssembly();
        if (!string.IsNullOrEmpty(AssemblyName))
        {
            try
            {
                // Find an assembly in the current AppDomain with the given name
                appHostAssembly = Assembly.Load(AssemblyName);
                if (appHostAssembly == null)
                {
                    throw new FileNotFoundException($"No assembly with name '{AssemblyName}' exists in the current AppDomain.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load AppHost assembly '{AssemblyName}' specified in {nameof(DistributedApplicationOptions)}.", ex);
            }
        }
        return appHostAssembly;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateProperty(this.SolutionName,
            new ValidationContext(this, null, null) { MemberName = "SolutionName" },
            results);
        Validator.TryValidateProperty(this.SolutionDir,
            new ValidationContext(this, null, null) { MemberName = "SolutionDir" },
            results);
        return results;
    }

    public string? ApplicationHostConfig => 
        (string.IsNullOrEmpty(SolutionDir) || 
            (string.IsNullOrEmpty(SolutionName) ||
                SolutionName.IndexOfAny(Path.GetInvalidFileNameChars())!=-1
            )
        ) ? null :
        System.IO.Path.Combine(
            SolutionDir,
            ".vs",
            SolutionName,
            "config",
            "applicationhost.config");
}
