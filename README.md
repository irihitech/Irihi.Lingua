# Irihi.Lingua

[English](README.md) | [中文](README.zh-CN.md)

[![NuGet](https://img.shields.io/nuget/v/Irihi.Lingua)](https://www.nuget.org/packages/Irihi.Lingua)

A C# source generator that turns `.resx` or `.json` resource files into a strongly-typed, reactive Avalonia i18n manager.
Each resource key becomes an `IObservable<string?>` property that pushes a new value whenever the active culture changes — no manual `INotifyPropertyChanged` wiring required.

## Special Thanks

Special thanks to [`sylinko/everywhere`](https://github.com/sylinko/everywhere) — this project was inspired by it.

## Highlights

- NativeAOT-compatible.
- Designed and optimized for Avalonia.
- Usable from both XAML and ViewModel code.
- Decentralized i18n managers: define separate resource managers in different classes and choose the granularity that fits your app — per feature, module, screen, or any other boundary you prefer.
- Supports both `.resx` (XML) and `.json` resource formats.

## Installation

Add the NuGet package to your project:

```xml
<PackageReference Include="Irihi.Lingua" />
```

The package bundles both the runtime library and the Roslyn source generator. No separate analyzer reference is needed.

## Quick Start

### 1. Create your `.resx` files

Create a base resource file and one file per culture you want to support.
The culture variant files must follow the `<BaseName>.<culture>.resx` naming convention.

`Resources/Strings.resx` (default / invariant culture):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="App_Title" xml:space="preserve">
    <value>My Application</value>
  </data>
  <data name="Greeting_Message" xml:space="preserve">
    <value>Hello!</value>
  </data>
</root>
```

`Resources/Strings.zh-Hans.resx` (Simplified Chinese):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="App_Title" xml:space="preserve">
    <value>我的应用程序</value>
  </data>
  <data name="Greeting_Message" xml:space="preserve">
    <value>你好！</value>
  </data>
</root>
```

### 2. Add the resource files as `AdditionalFiles`

The generator reads all `.resx` files listed as `AdditionalFiles` in your project file.
You can include them individually or use a wildcard to add every resource file in a folder.

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.resx" />
  <AdditionalFiles Include="Resources\Strings.zh-Hans.resx" />

  <!-- Or include all .resx files under Resources -->
  <AdditionalFiles Include="Resources\**\*.resx" />
</ItemGroup>
```

### 3. Declare the language manager

Apply `[LinguaManager]` to a `partial class`, pointing it at the base `.resx` file.
The source generator fills in the rest at build time.
The class name is entirely up to you, and you can define as many `[LinguaManager]` classes in one application as you need — each one is controlled by you.

```csharp
[LinguaManager("./Resources/Strings.resx")]
public partial class LanguageManager;
```

### 4. Subscribe to observable strings

Each resource key is exposed as an `IObservable<string?>` property on the singleton `Instance`.
Subscribers receive the current value immediately (behaviour-subject semantics) and then every subsequent update.

```csharp
using System.Globalization;

// Subscribe — current value is emitted immediately on Subscribe
using var titleSub = LanguageManager.Instance.App_Title.Subscribe(
    title => Console.WriteLine($"Title: {title}"));

using var greetingSub = LanguageManager.Instance.Greeting_Message.Subscribe(
    msg => Console.WriteLine($"Greeting: {msg}"));
```

### 5. Switch the active culture

Call `UpdateCulture` at any time to push new values to all active subscribers.
The method walks the culture hierarchy (e.g. `zh-Hans-CN` → `zh-Hans` → invariant) until a matching resource file is found.

```csharp
// Switch to Simplified Chinese
LanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

// Revert to the default (invariant) culture
LanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);
```

## JSON Source Format

In addition to `.resx`, **`.json` files are also supported**.  
Just point `[LinguaManager]` at a `.json` file — the file extension tells the generator which format to use.

### Nested object flattening

Nested JSON objects are **flattened into underscore-separated property names**.  
Only leaf values of type `string` become resource keys — numbers, booleans, and `null` are ignored.

`Resources/Strings.json`:
```json
{
  "a": "Content A",
  "b": "Content B",
  "c": {
    "x": "Content X",
    "y": "Content Y",
    "z": {
      "m": "Content M",
      "n": "Content N"
    }
  }
}
```

Generated properties: `a`, `b`, `c_x`, `c_y`, `c_z_m`, `c_z_n`

### Culture variants

Culture-specific JSON files follow the same `<BaseName>.<culture>.json` convention as `.resx`:

```
Resources/Strings.json              ← default / invariant
Resources/Strings.zh-Hans.json      ← Simplified Chinese
```

### Registration

Add JSON files as `AdditionalFiles` alongside your `.resx` files:

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.json" />
  <AdditionalFiles Include="Resources\Strings.zh-Hans.json" />

  <!-- Or include all .json files -->
  <AdditionalFiles Include="Resources\**\*.json" />
</ItemGroup>
```

### Manager declaration

Use a separate manager class for JSON resources (or replace your `.resx` manager entirely):

```csharp
[LinguaManager("./Resources/Strings.json")]
public partial class JsonLanguageManager;
```

### Usage

The generated API is identical — `Instance`, `UpdateCulture`, `Keys`, and observable properties work the same way:

```csharp
// { "app": { "title": "Hello" } } → app_title
using var sub = JsonLanguageManager.Instance.app_title.Subscribe(
    t => Console.WriteLine(t));

JsonLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
```

### Mixing RESX and JSON

You can define as many `[LinguaManager]` classes as you need — one project can use both `.resx` and `.json` managers side by side, each with its own resource files. The generators run independently and produce completely separate output types.

---

## Avalonia Usage

### ViewModel + stream binding (`^`)

Expose the observable string properties from your view-model and bind them in XAML using the `^` stream-binding operator.

```csharp
public class MainWindowViewModel
{
    public IObservable<string?> AppTitle       => LanguageManager.Instance.App_Title;
    public IObservable<string?> GreetingMessage => LanguageManager.Instance.Greeting_Message;
}
```

```xml
<TextBlock Text="{Binding AppTitle^}" />
<TextBlock Text="{Binding GreetingMessage^}" />
```

### `TranslateExtension` markup extension

For direct XAML bindings without a view-model wrapper, use the `Translate` markup extension together with the generated `Keys` nested class.

`TranslateExtension` is registered under the standard Avalonia XML namespace (`https://github.com/avaloniaui`) via `XmlnsDefinition`, so no additional namespace declaration is required.
Add only a `local:` alias for the namespace that contains your `LanguageManager`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:local="using:YourAppNamespace">
  
</Window>
```

Then use the extension:
```xml
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.App_Title}}" />
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.Greeting_Message}}" />
```

The extension resolves the `LinguaKey` from the `Keys` class, looks up the corresponding `IObservable<string?>` and converts it into an Avalonia binding that updates automatically when the culture changes.

### `FormatTranslateExtension` markup extension

Use `FormatTranslateExtension` (or `FormatTranslate` in XAML) when the resource is a format string (for example, `"Page {0} {1}"`) and you need to combine localized text with dynamic values.

`FormatKey` points to the format template, and nested `TranslateEntry` elements provide arguments in order:

- `TranslateEntry Key="..."` uses another localized key as an argument.
- `TranslateEntry Binding="..."` uses a normal Avalonia binding as an argument.

```xml
<TextBlock>
    <TextBlock.Text>
        <FormatTranslate FormatKey="{x:Static local:LanguageManager+Keys.Page_Template}">
            <TranslateEntry Binding="{Binding #page.Value}" />
            <TranslateEntry Key="{x:Static local:LanguageManager+Keys.Greeting_Message}" />
        </FormatTranslate>
    </TextBlock.Text>
</TextBlock>
```

In this example, `{0}` is filled by `#page.Value` and `{1}` is filled by `Greeting_Message`.
When either the current culture changes or any bound argument value changes, the final text is recomputed automatically.

### CulturePicker — built-in culture switcher

`CulturePicker` is a `TemplatedControl` that provides a ready-to-use `ComboBox` for switching cultures.
It drives one or more `ILinguaManager` instances and exposes a `CultureChanged` routed event.

#### XAML with binding

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

#### XAML inline definition

Cultures can also be defined directly in XAML — `LinguaCulture` has a `[Content]` property and a `TypeConverter` so you can write culture names as inner text:

```xml
<lingua:CulturePicker Managers="{Binding Managers}">
    <lingua:CulturePicker.Cultures>
        <lingua:LinguaCulture DisplayName="English" ShortDisplayName="EN">en</lingua:LinguaCulture>
        <lingua:LinguaCulture DisplayName="简体中文" ShortDisplayName="中文">zh-Hans</lingua:LinguaCulture>
    </lingua:CulturePicker.Cultures>
</lingua:CulturePicker>
```

#### CultureChanged event

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
