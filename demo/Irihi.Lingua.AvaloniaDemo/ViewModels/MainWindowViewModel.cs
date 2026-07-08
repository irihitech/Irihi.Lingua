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
        var currentCulture = LanguageManager.Instance.CurrentCulture;
    }
}
