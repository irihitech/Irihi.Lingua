# 快速开始

[文档目录](../README.md) | [English](../en/getting-started.md) | 中文

在应用项目中安装 NuGet 包：

```xml
<PackageReference Include="Irihi.Lingua" Version="1.1.0" />
```

包内已包含运行时库和源生成器，不需要单独添加 analyzer 引用。

## 1. 创建你的 `.resx` 文件

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

## 2. 将资源文件作为 `AdditionalFiles` 添加

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

## 3. 声明语言管理器

将 `[LinguaManager]` 应用于一个 `partial class`，并指向基础 `.resx` 文件。
源生成器会在构建时补全剩余代码。
类名完全由你自己决定，而且在一个应用中定义多少个 `[LinguaManager]` 类都可以——全部由你控制。

```csharp
using Irihi.Lingua;

[LinguaManager("./Resources/Strings.resx")]
public partial class LanguageManager;
```

## 4. 订阅响应式字符串

每个资源键都会以 `IObservable<string?>` 属性的形式暴露在单例 `Instance` 上。
订阅者会立即收到当前值（行为主体语义），之后每次更新都会继续收到通知。
示例使用本页末尾定义的 `ConsoleObserver`，不需要额外安装响应式库。

```csharp
using System.Globalization;

// 订阅后会立即收到当前值
using var titleSub = LanguageManager.Instance.App_Title.Subscribe(
    new ConsoleObserver());

using var greetingSub = LanguageManager.Instance.Greeting_Message.Subscribe(
    new ConsoleObserver());
```

## 5. 切换当前文化

随时调用 `UpdateCulture`，即可向所有活动订阅者推送新值。
该方法会沿着文化层级查找（例如 `zh-Hans-CN` → `zh-Hans` → 不变文化），直到找到匹配的资源文件。

```csharp
// 切换到简体中文
LanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));

// 恢复为默认（不变）文化
LanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);
```

## 控制台示例中的观察者

`IObservable<T>.Subscribe` 接受 `IObserver<T>`。不再需要更新时，应释放返回的订阅。Avalonia 流绑定会管理自己的订阅，详见 [Avalonia 绑定](avalonia-bindings.md)。

```csharp
using System;

public sealed class ConsoleObserver : IObserver<string?>
{
    public void OnNext(string? value) => Console.WriteLine(value);
    public void OnError(Exception error) => Console.Error.WriteLine(error);
    public void OnCompleted() { }
}
```

相关文档：[快速开始](getting-started.md) · [JSON 资源](json-resources.md) · [Avalonia 绑定](avalonia-bindings.md) · [格式化](formatting.md) · [CulturePicker](culture-picker.md)。
