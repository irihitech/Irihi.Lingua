# JSON 源文件格式

[文档目录](../README.md) | [English](../en/json-resources.md) | 中文

除了 `.resx` 之外，**`.json` 文件也同样支持**。
只需将 `[LinguaManager]` 指向一个 `.json` 文件——文件扩展名会自动告诉生成器使用哪种格式。

## 嵌套对象扁平化

嵌套的 JSON 对象会被**扁平化为以下划线分隔的属性名**。
只有类型为 `string` 的叶子值才会成为资源键——数字、布尔值和 `null` 会被忽略。

`Resources/Strings.json`：
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

生成的属性：`a`、`b`、`c_x`、`c_y`、`c_z_m`、`c_z_n`

## 文化变体

文化特定的 JSON 文件遵循与 `.resx` 相同的 `<BaseName>.<culture>.json` 命名约定：

```
Resources/Strings.json              ← 默认 / 不变文化
Resources/Strings.zh-Hans.json      ← 简体中文
```

## 注册

将 JSON 文件作为 `AdditionalFiles` 与 `.resx` 文件一同添加：

```xml
<ItemGroup>
  <AdditionalFiles Include="Resources\Strings.json" />
  <AdditionalFiles Include="Resources\Strings.zh-Hans.json" />

  <!-- 或者包含所有 .json 文件 -->
  <AdditionalFiles Include="Resources\**\*.json" />
</ItemGroup>
```

## 声明管理器

为 JSON 资源单独创建一个管理器类（也可以完全替换掉 `.resx` 管理器）：

```csharp
using Irihi.Lingua;

[LinguaManager("./Resources/Strings.json")]
public partial class JsonLanguageManager;
```

## 使用方式

生成的 API 完全相同——`Instance`、`UpdateCulture`、`Keys` 和可观察属性用法一致：
控制台订阅使用[快速开始](getting-started.md)中的 `ConsoleObserver`。

```csharp
// 上面 JSON 中的 c.z.m 会生成 c_z_m。
using var sub = JsonLanguageManager.Instance.c_z_m.Subscribe(
    new ConsoleObserver());

JsonLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
```

## RESX 与 JSON 混用

你可以定义任意多个 `[LinguaManager]` 类——一个项目可以同时使用 `.resx` 和 `.json` 管理器，各自拥有独立的资源文件。两个生成器独立运行，产生完全分离的输出类型。


相关文档：[快速开始](getting-started.md) · [JSON 资源](json-resources.md) · [Avalonia 绑定](avalonia-bindings.md) · [格式化](formatting.md) · [CulturePicker](culture-picker.md)。
