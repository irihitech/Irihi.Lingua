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
