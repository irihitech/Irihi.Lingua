# Avalonia bindings

[Documentation](../README.md) | English | [中文](../zh-CN/avalonia-bindings.md)

## ViewModel + stream binding (`^`)

Expose the observable string properties from your view-model and bind them in XAML using the `^` stream-binding operator.

```csharp
public class MainWindowViewModel
{
    public IObservable<string?> AppTitle       => LanguageManager.Instance.App_Title;
    public IObservable<string?> GreetingMessage => LanguageManager.Instance.Greeting_Message;
}
```

Set the view's `DataContext` to your view model. For compiled bindings, declare its type with `x:DataType`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:YourAppNamespace"
        x:DataType="vm:MainWindowViewModel">
    <StackPanel>
        <TextBlock Text="{Binding AppTitle^}" />
        <TextBlock Text="{Binding GreetingMessage^}" />
    </StackPanel>
</Window>
```

## Switch text sources with business state

Use `LinguaSwitchObservable` to keep a stable binding object and replace its
`Source` when business state changes. For example, assume the manager has `Start`
and `Stop` resources:

```csharp
public class TaskViewModel
{
    public LinguaSwitchObservable ActionText { get; } =
        new(LanguageManager.Instance.Start);

    // Call when business state changes, for example from an IsRunning setter.
    public void UpdateState(bool isRunning)
    {
        ActionText.Source = isRunning
            ? LanguageManager.Instance.Stop
            : LanguageManager.Instance.Start;
    }
}
```

```xml
<Button Content="{Binding ActionText^}" />
```

Switching state unsubscribes the old source, subscribes the new one and immediately
displays its current text. Subsequent language changes follow the selected resource.
Switching back also picks up that resource's latest text in the current language.
The `ActionText` instance stays the same, so changing its source does not require
a `PropertyChanged` notification.

`Source` accepts any `IObservable<string?>`, including
`LinguaObservableString.FromLiteral(...)` and formatted streams created with
`CreateFormat().Arg(...).Build()`. Assigning the same source instance does not
resubscribe. A null source is rejected, but null text values are supported.

The container connects to the source on its first subscription and disconnects
when its last observer leaves. Multiple observers share one source subscription.
For a custom source that does not emit immediately, it waits for the first value.
Until then, new subscribers do not receive cached text from the previous source;
an existing UI retains its last displayed text. Source completion and errors do
not terminate the container or prevent later switches, matching the formatting
container's treatment of long-lived localization streams. Notifications run on
the updating thread; change state and language on the UI thread when binding to UI.

## `TranslateExtension` markup extension

For direct XAML bindings without a view-model wrapper, use the `Translate` markup extension together with the generated `Keys` nested class.

`TranslateExtension` is registered under the standard Avalonia XML namespace (`https://github.com/avaloniaui`) via `XmlnsDefinition`, so no additional namespace declaration is required.
Add only a `local:` alias for the namespace that contains your `LanguageManager`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:YourAppNamespace">
</Window>
```

Then use the extension:
```xml
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.App_Title}}" />
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.Greeting_Message}}" />
```

The extension resolves the `LinguaKey` from the `Keys` class, looks up the corresponding `IObservable<string?>` and converts it into an Avalonia binding that updates automatically when the culture changes.

See also: [Getting started](getting-started.md) · [JSON resources](json-resources.md) · [Avalonia bindings](avalonia-bindings.md) · [Formatting](formatting.md) · [CulturePicker](culture-picker.md).
