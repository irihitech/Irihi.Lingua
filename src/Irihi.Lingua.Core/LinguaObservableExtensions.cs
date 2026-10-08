namespace Irihi.Lingua;

/// <summary>
/// General-purpose helpers for Lingua observables.
/// </summary>
public static class LinguaObservableExtensions
{
    /// <summary>
    /// Wraps an <see cref="IObservable{T}"/> of any element type as an
    /// <see cref="IObservable{T}"/> of object, boxing value-type elements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// .NET generic covariance only covers reference conversions, so an
    /// observable of a value type (e.g. <c>IObservable&lt;int&gt;</c>) is not
    /// recognized automatically by
    /// <see cref="LinguaFormatExtensions.Format(ILinguaManager, IObservable{string?}, object?[])"/>.
    /// Lingua's own observables (resource keys) are recognized without boxing;
    /// use this method for custom value-type observables from other libraries
    /// (e.g. ReactiveUI's <c>WhenAnyValue</c>).  Live-property arguments created
    /// with <see cref="PropertyChangeExtensions.Property"/> never need boxing.
    /// </para>
    /// <para>
    /// Reference-type observables are returned unchanged.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The element type of <paramref name="source"/>.</typeparam>
    /// <param name="source">The observable to adapt. Must not be <c>null</c>.</param>
    /// <returns>
    /// An <see cref="IObservable{T}"/> of object emitting the same values
    /// (value-type values boxed), with the same subscription semantics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is <c>null</c>.
    /// </exception>
    public static IObservable<object?> Box<T>(this IObservable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source as IObservable<object?> ?? new BoxedObservable<T>(source);
    }
}
