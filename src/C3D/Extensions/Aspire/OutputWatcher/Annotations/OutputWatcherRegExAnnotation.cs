using Aspire.Hosting.ApplicationModel;
using System.Text.RegularExpressions;

namespace C3D.Extensions.Aspire.OutputWatcher.Annotations;

public class OutputWatcherRegExAnnotation : OutputWatcherAnnotationBase, IValueProvider
{
    private readonly Regex matcher;

    public OutputWatcherRegExAnnotation(
        Regex matcher,
        bool isSecret,
        string? key = null)
        : base(isSecret, key)
    {
        this.matcher = matcher;
    }

    public override string PredicateName => matcher.ToString();

    public Func<OutputWatcherRegExAnnotation, ValueTask<string?>> ValueFunc { get; set; } =
        static async annotation =>
            await Task.FromResult(annotation.properties["Match"]?.ToString());

    public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) => await ValueFunc(this);

    public Func<Group, KeyValuePair<string, object>?> GroupValueFunc { get; set; } =
        static group => new(group.Name, group.Value);

    public override bool IsMatch(string message)
    {
        var match = matcher.Match(message);
        if (match.Success)
        {
            properties["Match"] = match.Value;
            foreach (Group group in match.Groups)
            {
                var kvp = GroupValueFunc(group);
                if (kvp is not null)
                {
                    if (kvp.Value.Value is null)
                    {
                        properties.Remove(kvp.Value.Key);
                    }
                    else
                    {
                        properties[kvp.Value.Key] = kvp.Value.Value;
                    }
                }
            }
        }
        return match.Success;
    }
}