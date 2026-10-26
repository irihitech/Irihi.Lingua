# Irihi.Lingua

English | [中文](https://github.com/irihitech/Irihi.Lingua/blob/main/README.zh-CN.md)

[![NuGet](https://img.shields.io/nuget/v/Irihi.Lingua)](https://www.nuget.org/packages/Irihi.Lingua)

Turn `.resx` and `.json` resources into strongly typed, observable strings for Avalonia.
Switch the active culture to update localized text in XAML and C#.

## Highlights

- Designed for Avalonia, with stream bindings and translation markup extensions.
- Independent language managers per feature, module, or screen.
- Reactive formatting with constant, observable, property, and computed arguments.
- NativeAOT-compatible, verified on .NET 8 and .NET 10 for Windows and Linux x64.

## Installation

```xml
<PackageReference Include="Irihi.Lingua" Version="1.1.0" />
```

The package includes the runtime library and Roslyn source generator.

## Documentation

Start with [Getting started](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/en/getting-started.md), or browse the [bilingual documentation index](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/README.md).

| Guide | Contents |
|---|---|
| [Getting started](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/en/getting-started.md) | RESX resources, manager generation, subscriptions, and culture switching |
| [JSON resources](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/en/json-resources.md) | Nested keys, culture variants, and mixing resource formats |
| [Avalonia bindings](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/en/avalonia-bindings.md) | ViewModel stream bindings and the Translate markup extension |
| [Formatting](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/en/formatting.md) | FormatTranslate, CreateFormat, dynamic arguments, and Refresh |
| [CulturePicker](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/en/culture-picker.md) | Theme setup, culture lists, bindings, and events |

## Examples and verification

- [Avalonia demo](https://github.com/irihitech/Irihi.Lingua/tree/main/demo/Irihi.Lingua.AvaloniaDemo)
- [Console demo](https://github.com/irihitech/Irihi.Lingua/tree/main/demo/Irihi.Lingua.Demo)
- [NativeAOT smoke checks](https://github.com/irihitech/Irihi.Lingua/blob/main/test/Irihi.Lingua.AotSmoke/README.md)

## Special thanks

Inspired by [sylinko/everywhere](https://github.com/sylinko/everywhere).

[MIT License](https://github.com/irihitech/Irihi.Lingua/blob/main/LICENSE)
