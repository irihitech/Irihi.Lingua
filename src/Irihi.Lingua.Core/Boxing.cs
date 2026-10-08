namespace Irihi.Lingua;

/// <summary>
/// Adapts an <see cref="IObserver{T}"/> of object to any element type by
/// boxing values on the way through.  Used by
/// <see cref="LinguaFormatBuilder.Arg{T}(IObservable{T})"/>, where the element
/// type is statically known at the call site, so no runtime type detection is
/// needed.
/// </summary>
internal sealed class BoxingObserver<T>(IObserver<object?> inner) : IObserver<T>
{
    public void OnCompleted() => inner.OnCompleted();
    public void OnError(Exception error) => inner.OnError(error);
    public void OnNext(T value) => inner.OnNext(value);
}
