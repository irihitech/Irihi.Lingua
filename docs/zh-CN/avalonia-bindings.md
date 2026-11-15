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

## 根据业务状态切换文本源

使用 `LinguaSwitchObservable` 保持绑定对象不变，在业务状态变化时替换它的 `Source`。
例如，假设管理器包含 `Start` 和 `Stop` 两个资源：

```csharp
public class TaskViewModel
{
    public LinguaSwitchObservable ActionText { get; } =
        new(LanguageManager.Instance.Start);

    // 在业务状态变化时调用，例如从 IsRunning 属性的 setter 中调用。
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

切换状态时，容器退订旧源、订阅新源，并立即显示新源的当前文本。
此后切换语言，显示的文本跟随当前选中的资源更新；切回之前的状态时，也会拿到该资源在当前语言下的最新文本。
`ActionText` 实例始终不变，因此不需要为文本源切换触发 `PropertyChanged`。

`Source` 接受任何 `IObservable<string?>`，包括 `LinguaObservableString.FromLiteral(...)`
和 `CreateFormat().Arg(...).Build()` 生成的格式化文本流。这样可以将不同状态的格式化文案接入同一个容器。
赋回同一个源实例不会重新订阅，`null` 源不被允许，但源发出的文本可以为 `null`。

容器在第一个观察者订阅时才连接源，最后一个观察者退订时自动断开，多个观察者共享一个源订阅。
对不会立即发出值的自定义源，容器会等待其首次发出值；在此之前，新订阅者不会收到上一个源的缓存文本，已有 UI 则保留最后显示的文本。
源的完成和错误通知不会终止容器，之后仍可切换源；这与格式化容器消费长期本地化流的方式一致。
通知在触发更新的线程上传递，请在 UI 线程上更改业务状态和语言。

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
