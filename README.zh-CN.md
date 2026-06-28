# Irihi.Lingua

[English](README.md) | 中文

[![NuGet](https://img.shields.io/nuget/v/Irihi.Lingua)](https://www.nuget.org/packages/Irihi.Lingua)

Irihi.Lingua 是一个 C# 源生成器，可以将 `.resx` 资源文件转换为强类型、响应式的 Avalonia i18n 管理器。
每个资源键都会变成一个 `IObservable<string?>` 属性，当当前文化切换时自动推送新值，无需手动编写 `INotifyPropertyChanged`。

## 特别致谢

特别感谢 [`sylinko/everywhere`](https://github.com/sylinko/everywhere)，本项目的灵感来源于它。

## 亮点

- 支持 NativeAOT。
- 为 Avalonia 设计并优化。
- 可同时在 XAML 和 ViewModel 中使用。
- 去中心化的 i18n 管理方式：你可以在不同的类中定义独立的资源管理器，并按功能、模块、页面或任意你希望的边界来划分资源粒度。

## 安装

将 NuGet 包添加到你的项目中：

```xml
<PackageReference Include="Irihi.Lingua" />
```

该包同时包含运行时库和 Roslyn 源生成器，不需要单独添加 analyzer 引用。

## 快速开始

### 1. 创建你的 `.resx` 文件

先创建一个基础资源文件，再为你需要支持的每种文化创建一个文件。
文化版本文件必须遵循 `<BaseName>.<culture>.resx` 的命名约定。

`Resources/Strings.resx`（默认 / 不变文化）：
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

`Resources/Strings.zh-Hans.resx`（简体中文）：
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

### 2. 将资源文件作为 `AdditionalFiles` 添加

生成器会读取项目文件中列出的所有 `.resx` `AdditionalFiles`。
你可以逐个添加，也可以使用通配符把某个文件夹下的所有资源文件一起加入。

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.resx" />
  <AdditionalFiles Include="Resources\Strings.zh-Hans.resx" />

  <!-- 或者包含 Resources 下所有 .resx 文件 -->
  <AdditionalFiles Include="Resources\**\*.resx" />
</ItemGroup>
```

### 3. 声明语言管理器

将 `[LinguaManager]` 应用于一个 `partial class`，并指向基础 `.resx` 文件。
源生成器会在构建时补全剩余代码。
类名完全由你自己决定，而且在一个应用中定义多少个 `[LinguaManager]` 类都可以——全部由你控制。

```csharp
[LinguaManager("./Resources/Strings.resx")]
public partial class LanguageManager;
```

### 4. 订阅响应式字符串

每个资源键都会以 `IObservable<string?>` 属性的形式暴露在单例 `Instance` 上。
订阅者会立即收到当前值（行为主体语义），之后每次更新都会继续收到通知。

```csharp
using System.Globalization;

// 订阅后会立即收到当前值
using var titleSub = LanguageManager.Instance.App_Title.Subscribe(
    title => Console.WriteLine($"Title: {title}"));

using var greetingSub = LanguageManager.Instance.Greeting_Message.Subscribe(
    msg => Console.WriteLine($"Greeting: {msg}"));
```

### 5. 切换当前文化

随时调用 `UpdateCulture`，即可向所有活动订阅者推送新值。
该方法会沿着文化层级查找（例如 `zh-Hans-CN` → `zh-Hans` → 不变文化），直到找到匹配的资源文件。

```csharp
// 切换到简体中文
LanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

// 恢复为默认（不变）文化
LanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);
```

---

## Avalonia 用法

### ViewModel + 流绑定（`^`）

将视图模型中的响应式字符串属性暴露出来，并在 XAML 中使用 `^` 流绑定操作符进行绑定。

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

### `TranslateExtension` 标记扩展

如果你不想通过 ViewModel 包装，也可以直接使用 `Translate` 标记扩展，并配合生成的 `Keys` 嵌套类。

`TranslateExtension` 已通过 `XmlnsDefinition` 注册到标准 Avalonia XML 命名空间（`https://github.com/avaloniaui`），因此无需额外声明该命名空间。
只需要为包含 `LanguageManager` 的命名空间添加一个 `local:` 别名：

```xml
xmlns="https://github.com/avaloniaui"
xmlns:local="using:YourAppNamespace"
```

然后这样使用：
```xml
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.App_Title}}" />
<TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.Greeting_Message}}" />
```

该扩展会从 `Keys` 类中解析 `LinguaKey`，查找对应的 `IObservable<string?>`，并将其转换为 Avalonia 绑定，使其在文化变化时自动更新。

### `FormatTranslateExtension` 标记扩展

当资源值是格式化字符串（例如 `"Page {0} {1}"`），并且你希望把本地化文本与动态值组合时，可以使用 `FormatTranslateExtension`（在 XAML 中简写为 `FormatTranslate`）。

`FormatKey` 指向格式模板，内部的 `TranslateEntry` 按顺序提供参数：

- `TranslateEntry Key="..."`：把另一个本地化资源键作为参数。
- `TranslateEntry Binding="..."`：把普通 Avalonia 绑定结果作为参数。

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

在这个示例中，`{0}` 来自 `#page.Value`，`{1}` 来自 `Greeting_Message`。
当当前文化变化，或任意参数绑定值变化时，最终文本都会自动重新计算。

