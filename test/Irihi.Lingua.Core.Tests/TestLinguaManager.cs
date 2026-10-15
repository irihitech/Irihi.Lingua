using System.Globalization;

namespace Irihi.Lingua.Tests;

/// <summary>
/// A minimal <see cref="ILinguaManager"/> implementation for tests, mirroring
/// the shape of the generated managers: compile-time resources resolved with
/// parent-culture fallback, runtime overrides taking precedence, and
/// observable-per-key semantics with the invariant value as initial value.
/// </summary>
internal sealed class FakeLinguaManager : ILinguaManager
{
    private readonly Dictionary<string, LinguaObservableString> _observables = new(StringComparer.Ordinal);
    private readonly LinguaRuntimeResources _staticResources = new();
    private readonly LinguaRuntimeResources _runtimeResources = new();
    private readonly LinguaObservable<CultureInfo> _cultureChanges = new(string.Empty, CultureInfo.InvariantCulture);
    private CultureInfo _currentCulture = CultureInfo.InvariantCulture;

    public CultureInfo CurrentCulture => _currentCulture;

    public IObservable<CultureInfo> CultureChanges => _cultureChanges;

    public FakeLinguaManager Add(CultureInfo culture, params (string Key, string Value)[] entries)
    {
        _staticResources.Add(culture, entries.ToDictionary(e => e.Key, e => e.Value));

        // Mirrors the generated code: an observable is created once with the
        // invariant value as its initial value; later values only arrive
        // through UpdateCulture.
        foreach (var (key, _) in entries)
        {
            if (_observables.ContainsKey(key)) continue;

            var invariantDict = _staticResources.Resolve(CultureInfo.InvariantCulture);
            var initial = invariantDict is not null && invariantDict.TryGetValue(key, out var invariantValue)
                ? invariantValue
                : string.Empty;
            _observables[key] = new LinguaObservableString(key, initial);
        }

        return this;
    }

    public LinguaKey Keys(string key) => new(key, this);

    public void UpdateCulture(CultureInfo culture)
    {
        if (culture is null) culture = CultureInfo.InvariantCulture;

        _currentCulture = culture;
        _cultureChanges.OnNext(culture);

        var staticDict = _staticResources.Resolve(culture);
        var runtimeDict = _runtimeResources.Resolve(culture);
        if (staticDict is null && runtimeDict is null) return;

        foreach (var observable in _observables.Values)
        {
            if (runtimeDict is not null && runtimeDict.TryGetValue(observable.Key, out var runtimeValue))
                observable.OnNext(runtimeValue);
            else if (staticDict is not null && staticDict.TryGetValue(observable.Key, out var staticValue))
                observable.OnNext(staticValue);
        }
    }

    public IObservable<string?>? GetObservable(string key) =>
        _observables.TryGetValue(key, out var observable) ? observable : null;

    public void AddResources(CultureInfo culture, IReadOnlyDictionary<string, string> resources) =>
        _runtimeResources.Add(culture, resources);

    public IReadOnlyDictionary<CultureInfo, string> GetTranslations(LinguaObservableString observable)
    {
        ArgumentNullException.ThrowIfNull(observable);

        var result = new Dictionary<CultureInfo, string>();
        foreach (var pair in _staticResources.GetAllValuesForKey(observable.Key))
            result[pair.Key] = pair.Value;
        foreach (var pair in _runtimeResources.GetAllValuesForKey(observable.Key))
            result[pair.Key] = pair.Value;
        return result;
    }

    public void ClearRuntimeResources() => _runtimeResources.Clear();
}
