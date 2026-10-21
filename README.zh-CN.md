# Irihi.Lingua

[English](https://github.com/irihitech/Irihi.Lingua/blob/main/README.md) | 中文

[![NuGet](https://img.shields.io/nuget/v/Irihi.Lingua)](https://www.nuget.org/packages/Irihi.Lingua)

将 `.resx` 和 `.json` 资源生成强类型、可观察的 Avalonia 本地化字符串。
切换当前文化，即可更新 XAML 和 C# 中的本地化文本。

## 亮点

- 为 Avalonia 设计，支持流绑定和翻译标记扩展。
- 按功能、模块或页面定义独立的语言管理器。
- 支持常量、可观察对象、属性和计算型参数的响应式格式化。
- 支持 NativeAOT，已在 Windows、Linux x64 上验证 .NET 8 和 .NET 10。

## 安装

```xml
<PackageReference Include="Irihi.Lingua" Version="1.1.0" />
```

包内已包含运行时库和 Roslyn 源生成器。

## 文档

初次使用请从[快速开始](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/zh-CN/getting-started.md)入手，也可以浏览[中英文文档目录](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/README.md)。

| 指南 | 内容 |
|---|---|
| [快速开始](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/zh-CN/getting-started.md) | RESX 资源、管理器生成、订阅与文化切换 |
| [JSON 资源](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/zh-CN/json-resources.md) | 嵌套键、文化变体及资源格式混用 |
| [Avalonia 绑定](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/zh-CN/avalonia-bindings.md) | ViewModel 流绑定和 Translate 标记扩展 |
| [格式化](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/zh-CN/formatting.md) | FormatTranslate、CreateFormat、动态参数与 Refresh |
| [CulturePicker](https://github.com/irihitech/Irihi.Lingua/blob/main/docs/zh-CN/culture-picker.md) | 主题安装、文化列表、绑定与事件 |

## 示例与验证

- [Avalonia 示例](https://github.com/irihitech/Irihi.Lingua/tree/main/demo/Irihi.Lingua.AvaloniaDemo)
- [控制台示例](https://github.com/irihitech/Irihi.Lingua/tree/main/demo/Irihi.Lingua.Demo)
- [NativeAOT 验证](https://github.com/irihitech/Irihi.Lingua/blob/main/test/Irihi.Lingua.AotSmoke/README.md)

## 特别致谢

本项目的灵感来自 [sylinko/everywhere](https://github.com/sylinko/everywhere)。

[MIT 许可证](https://github.com/irihitech/Irihi.Lingua/blob/main/LICENSE)
