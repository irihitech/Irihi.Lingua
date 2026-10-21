# CulturePicker — built-in culture switcher

[Documentation](../README.md) | English | [中文](../zh-CN/culture-picker.md)

`CulturePicker` is a `TemplatedControl` that provides a ready-to-use `ComboBox` for switching cultures.
It drives one or more `ILinguaManager` instances and exposes a `CultureChanged` routed event.

> **Important:** The control's template is defined in `Themes/Generic.axaml`. You must
> include the theme style in your `App.axaml`, otherwise the picker will appear blank:
>
> ```xml
> <Application.Styles>
>     <StyleInclude Source="avares://Irihi.Lingua.Core/Themes/Generic.axaml" />
> </Application.Styles>
> ```

## XAML with binding

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:lingua="https://irihi.tech/lingua">

    <lingua:CulturePicker Managers="{Binding Managers}"
                          Cultures="{Binding Cultures}" />
</Window>
```

```csharp
public class MainWindowViewModel
{
    public IList<ILinguaManager> Managers { get; } = new List<ILinguaManager>
    {
        LanguageManager.Instance,
        JsonLanguageManager.Instance
    };

    public IList<LinguaCulture> Cultures { get; } = new List<LinguaCulture>
    {
        new() { Culture = CultureInfo.InvariantCulture, DisplayName = "English", ShortDisplayName = "EN" },
        new() { Culture = new CultureInfo("zh-Hans"), DisplayName = "Chinese (Simplified)", ShortDisplayName = "中文" }
    };
}
```

## XAML inline definition

Cultures can also be defined directly in XAML — `LinguaCulture` has a `[Content]` property and a `TypeConverter` so you can write culture names as inner text:

```xml
<lingua:CulturePicker Managers="{Binding Managers}">
    <lingua:CulturePicker.Cultures>
        <lingua:LinguaCulture DisplayName="English" ShortDisplayName="EN">en</lingua:LinguaCulture>
        <lingua:LinguaCulture DisplayName="简体中文" ShortDisplayName="中文">zh-Hans</lingua:LinguaCulture>
    </lingua:CulturePicker.Cultures>
</lingua:CulturePicker>
```

## CultureChanged event

```xml
<lingua:CulturePicker CultureChanged="OnCultureChanged" />
```

```csharp
private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
{
    Console.WriteLine($"Switched to {e.Culture?.DisplayText}");
}
```

| Property | Type | Description |
|---|---|---|
| `Managers` | `IList<ILinguaManager>?` | One or more managers to drive |
| `Cultures` | `IList<LinguaCulture>?` | Available cultures |
| `SelectedItem` | `LinguaCulture?` | Currently selected culture |
| `CultureChanged` | `RoutedEvent<CultureChangedEventArgs>` | Fires after selection changes |

| `LinguaCulture` property | Description |
|---|---|
| `CultureName` (Content) | Culture identifier string (e.g. `"zh-Hans"`) |
| `DisplayName` | Full display name shown in dropdown |
| `ShortDisplayName` | Compact name shown in selection box |
| `DisplayText` | Read-only: `DisplayName ?? NativeName` |
| `SelectionDisplayText` | Read-only: `ShortDisplayName ?? DisplayText` |

See also: [Getting started](getting-started.md) · [JSON resources](json-resources.md) · [Avalonia bindings](avalonia-bindings.md) · [Formatting](formatting.md) · [CulturePicker](culture-picker.md).
