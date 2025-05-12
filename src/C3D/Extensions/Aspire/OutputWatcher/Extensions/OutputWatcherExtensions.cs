using Aspire.Hosting.ApplicationModel;
using C3D.Extensions.Aspire.OutputWatcher;
using C3D.Extensions.Aspire.OutputWatcher.Annotations;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Aspire.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static partial class OutputWatcherExtensions
{
    public static IServiceCollection AddResourceOutputWatcher(this IServiceCollection services) =>
        services.InsertHostedService<ResourceOutputWatcherService>();

    public static OutputWatcherBuilder<TResource, OutputWatcherAnnotation> WithOutputWatcher<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        string match,
        StringComparison comparer = StringComparison.InvariantCulture,
        bool isSecret = false,
        string? key = null)
        where TResource : IResource => resourceBuilder.WithOutputWatcher(
            message => message.Equals(match, comparer), isSecret, key, match);

    internal static OutputWatcherBuilder<TResource, TAnnotation> WithOutputWatcher<TResource, TAnnotation>(this
        IResourceBuilder<TResource> resourceBuilder,
        Func<string?, TAnnotation> annotationFactory,
        string? key = null)
        where TResource : IResource
        where TAnnotation : OutputWatcherAnnotationBase
    {
        resourceBuilder.ApplicationBuilder.Services.AddResourceOutputWatcher();

        var annotation = annotationFactory(key);
        resourceBuilder.WithAnnotation(annotation);
        return new(resourceBuilder, annotation);
    }

    public static OutputWatcherBuilder<TResource, OutputWatcherRegExAnnotation> WithOutputWatcher<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        Regex matcher,
        bool isSecret = false,
        string? key = null)
        where TResource : IResource => resourceBuilder.WithOutputWatcher(
            k => new OutputWatcherRegExAnnotation(matcher, isSecret, k), key);

    public static OutputWatcherBuilder<TResource, OutputWatcherAnnotation> WithOutputWatcher<TResource>(this
        IResourceBuilder<TResource> resourceBuilder,
        Func<string, bool> predicate,
        bool isSecret = false,
        string? key = null,
        string? predicateDisplayName = null)
        where TResource : IResource
            => resourceBuilder.WithOutputWatcher(
                k => new OutputWatcherAnnotation(predicate, isSecret, k, predicateDisplayName), key);

    public sealed class OutputWatcherBuilder<TResource, TWatcherAnnotation> : IResourceBuilder<TResource>
        where TResource : IResource
        where TWatcherAnnotation : OutputWatcherAnnotationBase
    {
        internal OutputWatcherBuilder(IResourceBuilder<TResource> inner, TWatcherAnnotation annotation)
        {
            ResourceBuilder = inner;
            Annotation = annotation;
        }

        public IDistributedApplicationBuilder ApplicationBuilder => ResourceBuilder.ApplicationBuilder;

        public TResource Resource => ResourceBuilder.Resource;

        public TWatcherAnnotation Annotation { get; }

        public string Key => Annotation.Key;

        public IResourceBuilder<TResource> ResourceBuilder { get; }

        public IResourceBuilder<TResource> WithAnnotation<TAnnotation>(TAnnotation annotation,
            ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append)
            where TAnnotation : IResourceAnnotation
                => ResourceBuilder.WithAnnotation(annotation, behavior);
    }

    public static OutputWatcherBuilder<TResource, TAnnotation> WithValueProvider<TResource, TAnnotation>(this
        OutputWatcherBuilder<TResource, TAnnotation> builder,
        Func<OutputWatcherRegExAnnotation, ValueTask<string?>> valueFunc)
        where TResource : IResource
        where TAnnotation : OutputWatcherRegExAnnotation
    {
        builder.Annotation.ValueFunc = valueFunc;
        return builder;
    }

    public static OutputWatcherAnnotationValueProvider<TAnnotation> GetValueProvider<TResource, TAnnotation>(this
        OutputWatcherBuilder<TResource, TAnnotation> builder,
        string property,
        Func<object?, ValueTask<string?>>? formatter = null)
        where TResource : IResource
        where TAnnotation : OutputWatcherRegExAnnotation
            => new(builder.Resource, builder.Annotation, property, formatter);

    public static ReferenceExpression GetReferenceExpression<TResource, TAnnotation>(this
        OutputWatcherBuilder<TResource, TAnnotation> builder,
        string property,
        Func<object?, ValueTask<string?>>? formatter = null)
        where TResource : IResource
        where TAnnotation : OutputWatcherRegExAnnotation
            => ReferenceExpression.Create($"{builder.GetValueProvider(property, formatter)}");

    public static OutputWatcherBuilder<TResource, TAnnotation> CreateReferenceExpression<TResource, TAnnotation>(this
        OutputWatcherBuilder<TResource, TAnnotation> builder,
        string property,
        out ReferenceExpression referenceExpression,
        Func<object?, ValueTask<string?>>? formatter = null)
        where TResource : IResource
        where TAnnotation : OutputWatcherRegExAnnotation
    {
        referenceExpression = builder.GetReferenceExpression(property, formatter);
        return builder;
    }

    public static OutputWatcherBuilder<TResource, TAnnotation> OnMatched<TResource, TAnnotation>(this
        OutputWatcherBuilder<TResource, TAnnotation> builder,
        Func<OutputMatchedEvent, CancellationToken, Task> matchedAction)
        where TResource : IResource
        where TAnnotation : OutputWatcherAnnotationBase
    {
        var key = builder.Key;
        builder.ApplicationBuilder.Eventing.Subscribe<OutputMatchedEvent>(builder.Resource, async (o, c) =>
        {
            if (o.Key == key)
            {
                await matchedAction(o, c);
            }
        });
        return builder;
    }
}
