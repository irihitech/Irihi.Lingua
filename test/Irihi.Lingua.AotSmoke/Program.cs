using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Irihi.Lingua;
using Irihi.Lingua.Extensions;

namespace AotSmoke;

public static class Program
{
    private static int _checks;

    public static void Main()
    {
        Check(!RuntimeFeature.IsDynamicCodeSupported, "native runtime has no dynamic code");
        var manager = SmokeManager.Instance;
        var fr = new CultureInfo("fr-FR");
        var number = new LinguaObservable<decimal>("value", 1.5m);
        var model = new SmokeModel { Value = 2 };
        var computed = 3;
        var formatted = SmokeManager.Keys.Fmt.CreateFormat()
            .Arg(number)
            .Arg(model, nameof(SmokeModel.Value), m => m.Value)
            .Arg(() => computed)
            .Arg(4)
            .Arg(new CultureInfo("ja-JP"))
            .Build();
        string? current = null;
        using (formatted.Subscribe(new Observer(value => current = value)))
        {
            Equal("1.5/2/3/4/ja-JP", current, "all argument kinds");
            number.OnNext(2.5m);
            Equal("2.5/2/3/4/ja-JP", current, "generic value-type observable");
            model.Value = 5;
            Equal("2.5/5/3/4/ja-JP", current, "INPC getter");
            computed = 6;
            formatted.Refresh();
            Equal("2.5/5/6/4/ja-JP", current, "computed getter and Refresh");
            manager.UpdateCulture(fr);
            Equal("2,5/5/6/4/ja-JP", current, "missing resource key culture change");
        }
        number.OnNext(7.5m);
        model.Value = 8;
        using (formatted.Subscribe(new Observer(value => current = value)))
            Equal("7,5/8/6/4/ja-JP", current, "reattach current snapshots");

        using (manager.CreateFormat("Constant: {0:F1}").Arg(1.5m).Build()
                   .Subscribe(new Observer(value => current = value)))
            Equal("Constant: 1,5", current, "constant template entry");
        var template = LinguaObservableString.FromLiteral("Custom: {0:F1}");
        using (manager.CreateFormat((IObservable<string?>)template).Arg(1.5m).Build()
                   .Subscribe(new Observer(value => current = value)))
        {
            Equal("Custom: 1,5", current, "observable template entry");
            template.OnNext("Changed: {0:F1}");
            Equal("Changed: 1,5", current, "observable template update");
        }
        var jsonManager = JsonSmokeManager.Instance;
        using (JsonSmokeManager.Keys.Fmt.CreateFormat().Arg(1.5m).Build()
                   .Subscribe(new Observer(value => current = value)))
        {
            Equal("JSON: 1.5", current, "JSON generated manager");
            jsonManager.UpdateCulture(fr);
            Equal("JSON: 1,5", current, "JSON culture update");
        }

        var counter = new LinguaObservable<int>("counter", 0);
        var reentrant = manager.CreateFormat("{0}").Arg(counter).Build();
        var received = new List<string?>();
        using (reentrant.Subscribe(new Observer(value =>
               {
                   if (value == "1") counter.OnNext(2);
               })))
        using (reentrant.Subscribe(new Observer(received.Add)))
        {
            counter.OnNext(1);
            Equal("0,1,2", string.Join(",", received), "reentrant notification order");
        }

        Equal("ja-JP", new FormatTranslateConverter().Convert(
            new object?[] { "{0}", new CultureInfo("ja-JP") },
            typeof(string), null, CultureInfo.InvariantCulture) as string,
            "public converter CultureInfo argument");

        manager.UpdateCulture(CultureInfo.InvariantCulture);
        AppBuilder.Configure<SmokeApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
            .SetupWithoutStarting();
        var viewModel = new SmokeModel { Value = 1.5m };
        var view = new SmokeView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 200, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var xamlResult = view.FindControl<TextBlock>("XamlResult")!;
        var codeResult = view.FindControl<TextBlock>("CodeResult")!;
        Equal("Value: 1.5", xamlResult.Text, "compiled FormatTranslate XAML initial");
        Equal("Value: 1.5", codeResult.Text, "compiled observable binding initial");
        viewModel.Value = 2.5m;
        Dispatcher.UIThread.RunJobs();
        Equal("Value: 2.5", xamlResult.Text, "compiled FormatTranslate property update");
        Equal("Value: 2.5", codeResult.Text, "compiled observable property update");
        manager.UpdateCulture(fr);
        Dispatcher.UIThread.RunJobs();
        Equal("Value: 2,5", xamlResult.Text, "XAML culture signal");
        Equal("Value: 2,5", codeResult.Text, "C# format culture change");
        foreach (var missingKey in new[] { false, true })
        {
            var extension = new FormatTranslateExtension
            {
                FormatKey = SmokeManager.Keys.NumberTemplate,
                Items = [missingKey
                    ? new TranslateEntry { Key = new LinguaKey("Missing", manager) }
                    : new TranslateEntry()]
            };
            var text = new TextBlock { DataContext = viewModel };
            using var binding = text.Bind(TextBlock.TextProperty,
                (MultiBinding)extension.ProvideValue(null!));
            Dispatcher.UIThread.RunJobs();
            Equal("Value: (unset)", text.Text, missingKey ? "missing key fallback" : "empty entry fallback");
        }
        window.Close();
        Console.WriteLine($"PASS: {_checks} NativeAOT checks ({System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription})");
    }

    private static void Equal(string? expected, string? actual, string scenario) =>
        Check(expected == actual, $"{scenario}: expected '{expected}', got '{actual}'");

    private static void Check(bool condition, string scenario)
    {
        if (!condition) throw new InvalidOperationException(scenario);
        _checks++;
        Console.WriteLine($"OK: {scenario}");
    }
}

[LinguaManager("./Resources/Strings.resx")]
public partial class SmokeManager;

[LinguaManager("./Resources/Strings.json")]
public partial class JsonSmokeManager;

public sealed class SmokeApp : Application { }

public sealed partial class SmokeView : UserControl
{
    public SmokeView() => AvaloniaXamlLoader.Load(this);
}

public sealed class SmokeModel : INotifyPropertyChanged
{
    private decimal _value;
    public event PropertyChangedEventHandler? PropertyChanged;
    public IObservable<string?> Formatted { get; }

    public SmokeModel() =>
        Formatted = SmokeManager.Keys.NumberTemplate.CreateFormat()
            .Arg(this, nameof(Value), m => m.Value).Build();

    public decimal Value
    {
        get => _value;
        set
        {
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}

public sealed class Observer(Action<string?> next) : IObserver<string?>
{
    public void OnCompleted() { }
    public void OnError(Exception error) => throw error;
    public void OnNext(string? value) => next(value);
}
