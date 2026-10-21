# Formatting

[Documentation](../README.md) | English | [中文](../zh-CN/formatting.md)

## `FormatTranslateExtension` markup extension

Use `FormatTranslateExtension` (or `FormatTranslate` in XAML) to combine a localized format template with dynamic values. Add a `Page_Template` resource such as `"Page {0} of {1}"`, with a translated template in each culture variant. Use the namespace declarations from [Avalonia bindings](avalonia-bindings.md).

`FormatKey` points to the format template, and nested `TranslateEntry` elements provide arguments in order:

- `TranslateEntry Key="..."` uses another localized key as an argument.
- `TranslateEntry Binding="..."` uses a normal Avalonia binding as an argument.

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

In this example, `{0}` is filled by `#page.Value` and `{1}` by `#totalPages.Value`.
When either the current culture changes or any bound argument value changes, the final text is recomputed automatically.

Formatting uses the owning manager's active culture, so number and date arguments follow the selected language rather than the thread culture.

## `CreateFormat` from C# code

Start from a format-template key with `CreateFormat`, add arguments with `Arg`, and finish with `Build`. Store the result in the view model so bindings share the same observable:

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

Bind it in XAML like any other Lingua observable:

```xml
<TextBlock Text="{Binding PageText^}" />
```

### Manual refresh

`Build()` returns a `LinguaFormatObservable`, which implements `IObservable<string?>`. Its `Refresh()` method re-reads live-property and computed getters, formats with the manager's current culture, and notifies subscribers even when the result is unchanged. The console example uses the `ConsoleObserver` from [Getting started](getting-started.md):

```csharp
var total = 7;
var totalText = LanguageManager.Instance.CreateFormat("Total {0}")
    .Arg(() => total)
    .Build();
using var subscription = totalText.Subscribe(new ConsoleObserver());

total = 9;
totalText.Refresh();
```

### Updates and subscription lifetime

Observable arguments are push-only — `Refresh()` does not re-read them; constants are fixed at build time. The built observable behaves like a behavior subject: subscribing immediately emits the current formatted string, and every subsequent change re-emits a recomputed value — whenever the format template changes, a live argument changes, or the manager's active culture changes. Formatting uses the manager's `CurrentCulture`, so numbers and dates follow the selected language even when the resource variant omits the template key. Notifications are serialized; updates and subscriptions initiated from an observer callback are delivered after the current notification completes.

Dispose manual subscriptions when they are no longer needed. The observable attaches to its sources while it has subscribers and detaches when the last subscription is disposed.

### Argument types

`Arg` has four overloads, one per argument kind:

- `Arg(constant)` — a fixed value;
- `Arg(observable)` — any `IObservable<T>`; value types (e.g. ReactiveUI's `WhenAnyValue`) are boxed automatically, no adaptation needed;
- `Arg(source, propertyName, getter)` — a live property: the combiner subscribes the source's `PropertyChanged` event directly and re-reads the property through the getter delegate — no reflection, no expression trees, keeping it trimmer- and NativeAOT-friendly. Setting a property to its current value does not re-emit (identical formatted results are suppressed), and `PropertyChanged` events with an empty or `null` name are honored as "all properties may have changed";
- `Arg(getter)` — a computed, read-on-demand value: read once on first subscription and on every `Refresh()`. The source does not need to implement `INotifyPropertyChanged` — use it for plain properties or external state without change notifications.

### Template sources

For a custom or constant template, start the chain from the manager with `CreateFormat` instead:

```csharp
var text = LanguageManager.Instance.CreateFormat("Page {0} of {1}")
    .Arg(3)
    .Arg(10)
    .Build();
```

`CreateFormat` also accepts an `IObservable<string?>` template source (e.g. `LinguaObservableString.FromLiteral` or a template composed at runtime). For templates that come from the manager's own resources, use `Keys.X.CreateFormat()` to start directly from the resource key.

```csharp
var template = LinguaObservableString.FromLiteral("Total {0}");
var text = LanguageManager.Instance.CreateFormat(template).Arg(10).Build();
```

The builder is frozen after `Build()`; further `Arg` calls throw. Calling `Build()` again creates an independent observable from the same arguments.

See also: [Getting started](getting-started.md) · [JSON resources](json-resources.md) · [Avalonia bindings](avalonia-bindings.md) · [Formatting](formatting.md) · [CulturePicker](culture-picker.md).
