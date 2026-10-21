# Avalonia 绑定

[文档目录](../README.md) | [English](../en/avalonia-bindings.md) | 中文

## ViewModel + 流绑定（`^`）

将视图模型中的响应式字符串属性暴露出来，并在 XAML 中使用 `^` 流绑定操作符进行绑定。

```csharp
public class MainWindowViewModel
{
    public IObservable<string?> AppTitle       => LanguageManager.Instance.App_Title;
    public IObservable<string?> GreetingMessage => LanguageManager.Instance.Greeting_Message;
}
```

将视图的 `DataContext` 设为对应的 ViewModel。使用编译绑定时，通过 `x:DataType` 声明其类型：

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

## `TranslateExtension` 标记扩展

如果你不想通过 ViewModel 包装，也可以直接使用 `Translate` 标记扩展，并配合生成的 `Keys` 嵌套类。

`TranslateExtension` 已通过 `XmlnsDefinition` 注册到标准 Avalonia XML 命名空间（`https://github.com/avaloniaui`），因此无需额外声明该命名空间。
只需要为包含 `LanguageManager` 的命名空间添加一个 `local:` 别名：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:YourAppNamespace">
</Window>
```

然后这样使用：
```xml
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.App_Title}}" />
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.Greeting_Message}}" />
```

该扩展会从 `Keys` 类中解析 `LinguaKey`，查找对应的 `IObservable<string?>`，并将其转换为 Avalonia 绑定，使其在文化变化时自动更新。

相关文档：[快速开始](getting-started.md) · [JSON 资源](json-resources.md) · [Avalonia 绑定](avalonia-bindings.md) · [格式化](formatting.md) · [CulturePicker](culture-picker.md)。
