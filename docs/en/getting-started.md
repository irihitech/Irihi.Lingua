# Getting started

[Documentation](../README.md) | English | [中文](../zh-CN/getting-started.md)

Install the package in your application project:

```xml
<PackageReference Include="Irihi.Lingua" Version="1.1.0" />
```

It includes the runtime library and source generator; no separate analyzer reference is needed.

## 1. Create your `.resx` files

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

## 2. Add the resource files as `AdditionalFiles`

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

## 3. Declare the language manager

Apply `[LinguaManager]` to a `partial class`, pointing it at the base `.resx` file.
The source generator fills in the rest at build time.
The class name is entirely up to you, and you can define as many `[LinguaManager]` classes in one application as you need — each one is controlled by you.

```csharp
using Irihi.Lingua;

[LinguaManager("./Resources/Strings.resx")]
public partial class LanguageManager;
```

## 4. Subscribe to observable strings

Each resource key is exposed as an `IObservable<string?>` property on the singleton `Instance`.
Subscribers receive the current value immediately (behaviour-subject semantics) and then every subsequent update.
The examples use the `ConsoleObserver` defined below, so no additional reactive package is required.

```csharp
using System.Globalization;

// Subscribe — current value is emitted immediately on Subscribe
using var titleSub = LanguageManager.Instance.App_Title.Subscribe(
    new ConsoleObserver());

using var greetingSub = LanguageManager.Instance.Greeting_Message.Subscribe(
    new ConsoleObserver());
```

## 5. Switch the active culture

Call `UpdateCulture` at any time to push new values to all active subscribers.
The method walks the culture hierarchy (e.g. `zh-Hans-CN` → `zh-Hans` → invariant) until a matching resource file is found.

```csharp
// Switch to Simplified Chinese
LanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

// Revert to the default (invariant) culture
LanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);
```

## Observer used by the console examples

`IObservable<T>.Subscribe` accepts an `IObserver<T>`. Dispose the returned subscription when you no longer need updates. Avalonia stream bindings handle their own subscriptions; see [Avalonia bindings](avalonia-bindings.md).

```csharp
using System;

public sealed class ConsoleObserver : IObserver<string?>
{
    public void OnNext(string? value) => Console.WriteLine(value);
    public void OnError(Exception error) => Console.Error.WriteLine(error);
    public void OnCompleted() { }
}
```

See also: [Getting started](getting-started.md) · [JSON resources](json-resources.md) · [Avalonia bindings](avalonia-bindings.md) · [Formatting](formatting.md) · [CulturePicker](culture-picker.md).
