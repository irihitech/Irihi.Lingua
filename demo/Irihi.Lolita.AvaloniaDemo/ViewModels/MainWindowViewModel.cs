using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Irihi.Lolita.AvaloniaDemo.ViewModels;

/// <summary>
/// Exposes the <see cref="LanguageManager"/> observable strings as bindable
/// <see cref="IObservable{T}"/> properties that Avalonia can subscribe to
/// directly in XAML using the <c>^</c> stream-binding operator.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private bool _isChinese;

    /// <summary>
    /// Observable title of the application window.
    /// Avalonia binds to this via <c>{Binding AppTitle^}</c>.
    /// </summary>
    public IObservable<string> AppTitle => LanguageManager.Instance.App_Title;

    /// <summary>
    /// Observable greeting message.
    /// Avalonia binds to this via <c>{Binding GreetingMessage^}</c>.
    /// </summary>
    public IObservable<string> GreetingMessage => LanguageManager.Instance.Greeting_Message;

    /// <summary>
    /// Observable label for the language-toggle button, so the button text
    /// itself updates reactively when the culture changes.
    /// Avalonia binds to this via <c>{Binding SwitchLanguageLabel^}</c>.
    /// </summary>
    public IObservable<string> SwitchLanguageLabel => LanguageManager.Instance.Switch_Language;

    /// <summary>
    /// Toggles the application language between Simplified Chinese and the
    /// default (invariant) culture, demonstrating live UI updates driven by
    /// <see cref="LolitaObservableString"/> without any manual property-change
    /// notifications in the view-model.
    /// </summary>
    [RelayCommand]
    private void ToggleCulture()
    {
        _isChinese = !_isChinese;
        LanguageManager.Instance.UpdateCulture(
            _isChinese
                ? new CultureInfo("zh-Hans")
                : CultureInfo.InvariantCulture);
    }
}
