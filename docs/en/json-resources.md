# JSON Source Format

[Documentation](../README.md) | English | [中文](../zh-CN/json-resources.md)

In addition to `.resx`, **`.json` files are also supported**.
Just point `[LinguaManager]` at a `.json` file — the file extension tells the generator which format to use.

## Nested object flattening

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

## Culture variants

Culture-specific JSON files follow the same `<BaseName>.<culture>.json` convention as `.resx`:

```
Resources/Strings.json              ← default / invariant
Resources/Strings.zh-Hans.json      ← Simplified Chinese
```

## Registration

Add JSON files as `AdditionalFiles` alongside your `.resx` files:

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.json" />
  <AdditionalFiles Include="Resources\Strings.zh-Hans.json" />

  <!-- Or include all .json files -->
  <AdditionalFiles Include="Resources\**\*.json" />
</ItemGroup>
```

## Manager declaration

Use a separate manager class for JSON resources (or replace your `.resx` manager entirely):

```csharp
using Irihi.Lingua;

[LinguaManager("./Resources/Strings.json")]
public partial class JsonLanguageManager;
```

## Usage

The generated API is identical — `Instance`, `UpdateCulture`, `Keys`, and observable properties work the same way:
Use the `ConsoleObserver` from [Getting started](getting-started.md) for a console subscription.

```csharp
// c.z.m in the JSON above becomes c_z_m.
using var sub = JsonLanguageManager.Instance.c_z_m.Subscribe(
    new ConsoleObserver());

JsonLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
```

## Mixing RESX and JSON

You can define as many `[LinguaManager]` classes as you need — one project can use both `.resx` and `.json` managers side by side, each with its own resource files. The generators run independently and produce completely separate output types.


See also: [Getting started](getting-started.md) · [JSON resources](json-resources.md) · [Avalonia bindings](avalonia-bindings.md) · [Formatting](formatting.md) · [CulturePicker](culture-picker.md).
