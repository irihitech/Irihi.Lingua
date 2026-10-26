# 格式化

[文档目录](../README.md) | [English](../en/formatting.md) | 中文

## `FormatTranslateExtension` 标记扩展

使用 `FormatTranslateExtension`（在 XAML 中简写为 `FormatTranslate`）将本地化模板与动态值组合。先添加 `Page_Template` 资源，例如 `"Page {0} of {1}"`，并在各个文化变体中提供翻译后的模板。命名空间声明见 [Avalonia 绑定](avalonia-bindings.md)。

`FormatKey` 指向格式模板，内部的 `TranslateEntry` 按顺序提供参数：

- `TranslateEntry Key="..."`：把另一个本地化资源键作为参数。
- `TranslateEntry Binding="..."`：把普通 Avalonia 绑定结果作为参数。

```xml
<StackPanel>
    <NumericUpDown Name="page" Value="1" />
    <NumericUpDown Name="totalPages" Value="10" />
    <TextBlock>
        <TextBlock.Text>
            <FormatTranslate FormatKey="{x:Static local:LanguageManager+Keys.Page_Template}">
                <TranslateEntry Binding="{Binding #page.Value}" />
                <TranslateEntry Binding="{Binding #totalPages.Value}" />
            </FormatTranslate>
        </TextBlock.Text>
    </TextBlock>
</StackPanel>
```

在这个示例中，`{0}` 来自 `#page.Value`，`{1}` 来自 `#totalPages.Value`。
当当前文化变化，或任意参数绑定值变化时，最终文本都会自动重新计算。

格式化使用所属 manager 的当前文化，因此数字和日期参数会跟随所选语言，而不是线程文化。

## 在 C# 代码中格式化（`CreateFormat`）

从格式模板键调用 `CreateFormat`，用 `Arg` 添加参数，再以 `Build` 结束。将结果保存在 ViewModel 中，让多个绑定共享同一个可观察对象：

```csharp
using System.ComponentModel;
using Irihi.Lingua;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private int _page = 1;
    private int _totalPages = 10;
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Page
    {
        get => _page;
        set
        {
            _page = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Page)));
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set
        {
            _totalPages = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPages)));
        }
    }

    public LinguaFormatObservable PageText { get; }

    public MainWindowViewModel() =>
        PageText = LanguageManager.Keys.Page_Template.CreateFormat()
            .Arg(this, nameof(Page), s => s.Page)
            .Arg(this, nameof(TotalPages), s => s.TotalPages)
            .Build();
}
```

在 XAML 中像其他 Lingua 可观察对象一样绑定：

```xml
<TextBlock Text="{Binding PageText^}" />
```

### 手动刷新

`Build()` 返回的 `LinguaFormatObservable` 实现了 `IObservable<string?>`。它的 `Refresh()` 方法会重读活属性和计算型 getter，按 manager 的当前文化重算，并在结果不变时仍通知订阅者。下面的控制台示例使用[快速开始](getting-started.md)中的 `ConsoleObserver`：

```csharp
var total = 7;
var totalText = LanguageManager.Instance.CreateFormat("Total {0}")
    .Arg(() => total)
    .Build();
using var subscription = totalText.Subscribe(new ConsoleObserver());

total = 9;
totalText.Refresh();
```

### 更新与订阅生命周期

可观察对象参数是纯推送的——`Refresh()` 不会重读它们；常量在构建时固化。合成后的可观察对象具有 BehaviorSubject 语义：订阅时立即发出当前格式化结果，之后在以下任一情况发生时重新发出：格式模板变化、任一活参数变化，或 manager 的当前文化变化。格式化使用 manager 的 `CurrentCulture`，因此即使资源变体缺少模板键，数字和日期的格式也会跟随所选语言。通知按顺序串行发送；在观察者回调中触发的更新和新订阅，会在当前通知完成后送达。

不再需要更新时，应释放手动创建的订阅。合成后的可观察对象在有订阅者时连接参数源，最后一个订阅释放后会断开连接。

### 参数类型

`Arg` 有四个重载，对应四种参数：

- `Arg(常量)` —— 固定值；
- `Arg(可观察对象)` —— 任意 `IObservable<T>`；值类型（如 ReactiveUI 的 `WhenAnyValue`）自动装箱，无需适配；
- `Arg(源, 属性名, 读取器)` —— 活属性：组合器直接订阅源的 `PropertyChanged` 事件并通过 getter 委托重新读取属性 —— 不使用反射、不使用表达式树，对裁剪和 NativeAOT 友好。属性被设置为相同值时不会重新发出（相同的格式化结果会被抑制），并支持 `PropertyChanged` 以空或 null 名称表示"所有属性可能已变化"的约定；
- `Arg(读取器)` —— 计算型、按需读取的值：首次订阅时读取一次，之后在每次 `Refresh()` 时重读。源不需要实现 `INotifyPropertyChanged`——适合普通属性或没有变更通知的外部状态。

### 模板来源

使用自定义或常量模板时，用 `CreateFormat` 从 manager 侧开始链：

```csharp
var text = LanguageManager.Instance.CreateFormat("Page {0} of {1}")
    .Arg(3)
    .Arg(10)
    .Build();
```

`CreateFormat` 也接受 `IObservable<string?>` 模板源（如 `LinguaObservableString.FromLiteral` 或运行时组合的模板）。模板来自 manager 自身资源时，可用 `Keys.X.CreateFormat()` 直接从资源键开始。

```csharp
var template = LinguaObservableString.FromLiteral("Total {0}");
var text = LanguageManager.Instance.CreateFormat(template).Arg(10).Build();
```

构建器在 `Build()` 后冻结，继续调用 `Arg` 会抛出异常。再次调用 `Build()` 会用相同参数创建独立的可观察对象。

相关文档：[快速开始](getting-started.md) · [JSON 资源](json-resources.md) · [Avalonia 绑定](avalonia-bindings.md) · [格式化](formatting.md) · [CulturePicker](culture-picker.md)。
