namespace Irihi.Lingua;

/// <summary>
/// A lightweight observable string that behaves like a <c>BehaviorSubject&lt;string?&gt;</c>:
/// it emits the current value immediately upon subscription and notifies all
/// subscribers whenever the value changes.
/// </summary>
/// <remarks>
/// This type is used by the code generated for classes marked with
/// <see cref="LinguaManagerAttribute"/> and does not require any additional
/// reactive library in consumer projects.
/// </remarks>
public sealed class LinguaObservableString : LinguaObservable<string?>
{
    /// <summary>
    /// Creates an <see cref="LinguaObservableString"/> from a literal string value,
    /// without tying it to any resource (.resx) key or <see cref="ILinguaManager"/>.
    /// </summary>
    /// <remarks>
    /// The returned observable immediately emits <paramref name="value"/> to new
    /// subscribers (behavior-subject semantics) and never notifies again unless
    /// <see cref="LinguaObservable{T}.OnNext"/> is called explicitly.
    /// This is useful when you need an <see cref="IObservable{String}"/> but the
    /// source is a hard-coded string rather than a localized resource — for example,
    /// exposing a constant label alongside localized observables in a ViewModel.
    /// </remarks>
    /// <param name="value">
    /// The string value to wrap.  A <c>null</c> value is allowed and results in an
    /// observable that emits <c>null</c> to subscribers.
    /// </param>
    /// <returns>
    /// A new <see cref="LinguaObservableString"/> whose <see cref="LinguaObservable{T}.Key"/>
    /// is an empty string (since no resource key is involved).
    /// </returns>
    public static LinguaObservableString FromLiteral(string? value) =>
        new(string.Empty, value);

    /// <summary>
    /// Initializes a new instance of <see cref="LinguaObservableString"/> with the given key and initial value.
    /// </summary>
    /// <param name="key">The resource key that this observable represents.</param>
    /// <param name="initialValue">The value to emit to new subscribers immediately.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c>.</exception>
    public LinguaObservableString(string key, string? initialValue)
        : base(key, initialValue)
    {
    }
}
