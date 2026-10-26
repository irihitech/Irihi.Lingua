# CulturePicker — 内置文化切换控件

[文档目录](../README.md) | [English](../en/culture-picker.md) | 中文

`CulturePicker` 是一个 `TemplatedControl`，提供开箱即用的 `ComboBox` 用于切换文化。
它可以驱动一个或多个 `ILinguaManager` 实例，并暴露 `CultureChanged` 路由事件。

> **注意：** 控件的模板定义在 `Themes/Generic.axaml` 中。必须在 `App.axaml` 中引入主题样式，否则控件将显示为空白：
>
> ```xml
> <Application.Styles>
>     <StyleInclude Source="avares://Irihi.Lingua.Core/Themes/Generic.axaml" />
> </Application.Styles>
> ```

## 通过 Binding 使用

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

## XAML 内联定义

Culture 也可以直接在 XAML 中定义——`LinguaCulture` 带有 `[Content]` 属性和 `TypeConverter`，因此可以直接将文化名称作为内部文本：

```xml
<lingua:CulturePicker Managers="{Binding Managers}">
    <lingua:CulturePicker.Cultures>
        <lingua:LinguaCulture DisplayName="English" ShortDisplayName="EN">en</lingua:LinguaCulture>
        <lingua:LinguaCulture DisplayName="简体中文" ShortDisplayName="中文">zh-Hans</lingua:LinguaCulture>
    </lingua:CulturePicker.Cultures>
</lingua:CulturePicker>
```

## CultureChanged 事件

```xml
<lingua:CulturePicker CultureChanged="OnCultureChanged" />
```

```csharp
private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
{
    Console.WriteLine($"已切换到 {e.Culture?.DisplayText}");
}
```

| 属性 | 类型 | 说明 |
|---|---|---|
| `Managers` | `IList<ILinguaManager>?` | 要驱动的一个或多个管理器 |
| `Cultures` | `IList<LinguaCulture>?` | 可选文化列表 |
| `SelectedItem` | `LinguaCulture?` | 当前选中的文化 |
| `CultureChanged` | `RoutedEvent<CultureChangedEventArgs>` | 选中项变化时触发 |

| `LinguaCulture` 属性 | 说明 |
|---|---|
| `CultureName`（Content） | 文化标识符字符串（例如 `"zh-Hans"`） |
| `DisplayName` | 下拉列表中显示的完整名称 |
| `ShortDisplayName` | 选中框中显示的简洁名称 |
| `DisplayText` | 只读：`DisplayName ?? NativeName` |
| `SelectionDisplayText` | 只读：`ShortDisplayName ?? DisplayText` |

相关文档：[快速开始](getting-started.md) · [JSON 资源](json-resources.md) · [Avalonia 绑定](avalonia-bindings.md) · [格式化](formatting.md) · [CulturePicker](culture-picker.md)。
