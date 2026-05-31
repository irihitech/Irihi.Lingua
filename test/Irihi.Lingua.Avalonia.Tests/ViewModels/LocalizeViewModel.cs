namespace Irihi.Lingua.Avalonia.Tests.ViewModels;

/// <summary>
/// ViewModel for <see cref="Views.LocalizeView"/>.
/// Mirrors <c>MainWindowViewModel</c> in the demo: each property exposes an
/// <see cref="IObservable{T}">IObservable&lt;string?&gt;</see> from the
/// <see cref="TestLanguageManager"/> singleton so that Avalonia's
/// <c>^</c> stream-binding operator can subscribe to it in XAML.
/// </summary>
public sealed class LocalizeViewModel
{
    /// <summary>Observable application title — bound via <c>{Binding AppTitle^}</c>.</summary>
    public IObservable<string?> AppTitle =>
        TestLanguageManager.Instance.App_Title;

    /// <summary>Observable greeting message — bound via <c>{Binding GreetingMessage^}</c>.</summary>
    public IObservable<string?> GreetingMessage =>
        TestLanguageManager.Instance.Greeting_Message;
}
