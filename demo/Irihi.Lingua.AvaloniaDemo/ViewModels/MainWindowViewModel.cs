using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Irihi.Lingua.AvaloniaDemo.ViewModels;

/// <summary>
/// Exposes both RESX and JSON <see cref="LanguageManager"/> observable strings
/// as bindable <see cref="IObservable{T}"/> properties that Avalonia can
/// subscribe to directly in XAML using the <c>^</c> stream-binding operator.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private bool _isChinese;
    private readonly IDisposable _cultureSub;

    /// <summary>
    /// Current culture display string, updated via <see cref="ILinguaManager.CultureChanges"/>.
    /// </summary>
    [ObservableProperty]
    private string? _currentCultureDisplay;

    public MainWindowViewModel()
    {
        // Subscribe to CultureChanges to update the display label
        _cultureSub = LanguageManager.Instance.CultureChanges.Subscribe(
            new DelegateObserver<CultureInfo>(c => CurrentCultureDisplay = c.EnglishName));
    }

    // ── RESX-based observables ───────────────────────────────────────────────

    public IObservable<string?> AppTitle => LanguageManager.Instance.App_Title;

    public IObservable<string?> GreetingMessage => LanguageManager.Instance.Greeting_Message;

    public IObservable<string?> SwitchLanguageLabel => LanguageManager.Instance.Switch_Language;

    // ── JSON-based observables (flattened from nested objects) ────────────────
    // { "app": { "title": "...", "greeting": "..." } } → app_title, app_greeting

    public IObservable<string?> JsonAppTitle => JsonLanguageManager.Instance.app_title;

    public IObservable<string?> JsonGreetingMessage => JsonLanguageManager.Instance.app_greeting;

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleCulture()
    {
        _isChinese = !_isChinese;
        var culture = _isChinese
            ? new CultureInfo("zh-Hans")
            : CultureInfo.InvariantCulture;

        LanguageManager.Instance.UpdateCulture(culture);
        JsonLanguageManager.Instance.UpdateCulture(culture);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegateObserver<T>(Action<T> onNext) : IObserver<T>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => onNext(value);
    }
}
