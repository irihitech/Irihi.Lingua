namespace Irihi.Lingua;

/// <summary>
/// Exposes a type-erased subscription for observables whose element type is a
/// value type.  .NET generic covariance only covers reference conversions, so
/// e.g. <c>IObservable&lt;int&gt;</c> cannot be matched as
/// <c>IObservable&lt;object?&gt;</c>; this interface lets the Lingua format
/// combiner subscribe to such sources without reflection.
/// </summary>
internal interface IBoxedObservable
{
    /// <summary>
    /// Subscribes an <see cref="IObserver{T}"/> of object that receives the
    /// observable's values (value-type values boxed) and its current value
    /// immediately, following the observable's own semantics.
    /// </summary>
    IDisposable SubscribeBoxed(IObserver<object?> observer);
}

/// <summary>
/// Adapts an <see cref="IObserver{T}"/> of object to any element type by
/// boxing values on the way through.
/// </summary>
internal sealed class BoxingObserver<T>(IObserver<object?> inner) : IObserver<T>
{
    public void OnCompleted() => inner.OnCompleted();
    public void OnError(Exception error) => inner.OnError(error);
    public void OnNext(T value) => inner.OnNext(value);
}

/// <summary>
/// Wraps any <see cref="IObservable{T}"/> as an <see cref="IObservable{T}"/> of
/// object, boxing value-type elements.  Used by
/// <see cref="LinguaObservableExtensions.Box{T}"/> and by
/// <see cref="IBoxedObservable"/> implementations.
/// </summary>
internal sealed class BoxedObservable<T>(IObservable<T> source) : IObservable<object?>, IBoxedObservable
{
    public IDisposable Subscribe(IObserver<object?> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return source.Subscribe(new BoxingObserver<T>(observer));
    }

    IDisposable IBoxedObservable.SubscribeBoxed(IObserver<object?> observer) =>
        Subscribe(observer);
}
