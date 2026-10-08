using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Irihi.Lingua.Avalonia.Tests.ViewModels;
using Irihi.Lingua.Avalonia.Tests.Views;
using Irihi.Lingua.Extensions;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

public class FormatTranslateExtensionTests
{
    private static (Window Window, TextBlock Input, TextBlock Page) CreateAndShowWindowForLocalizeFormat()
    {
        var vm = new LocalizeViewModel();
        var view = new LocalizeView { DataContext = vm };
        var window = new Window
        {
            Content = view,
            Width = 400,
            Height = 200,
            SizeToContent = SizeToContent.Manual
        };
        window.Show();

        Dispatcher.UIThread.RunJobs();

        var input = view.Find<TextBlock>("Input")!;
        var page = view.Find<TextBlock>("Page")!;

        return (window, input, page);
    }

    [AvaloniaFact]
    public void LocalizeFormat_AfterCultureSwitch_PageTextUpdatesWithLocalizedTemplateAndItems()
    {
        TestLanguageManager.Instance.Reset();
        var (window, input, page) = CreateAndShowWindowForLocalizeFormat();

        input.Text = "3";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello, Page 3", page.Text);

        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好, 第3页", page.Text);

        window.Close();
    }

    [AvaloniaFact]
    public void LocalizeFormat_AfterInputTextChange_PageTextUpdatesWithLatestValue()
    {
        TestLanguageManager.Instance.Reset();
        var (window, input, page) = CreateAndShowWindowForLocalizeFormat();

        input.Text = "1";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello, Page 1", page.Text);

        input.Text = "42";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello, Page 42", page.Text);

        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好, 第42页", page.Text);

        input.Text = "1";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好, 第1页", page.Text);

        window.Close();
    }

    [AvaloniaFact]
    public void LocalizeFormat_WhenInputTextIsNull_PageTextUsesEmptyPlaceholderAndStillReactsToCultureChanges()
    {
        TestLanguageManager.Instance.Reset();
        var (window, input, page) = CreateAndShowWindowForLocalizeFormat();

        input.Text = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello, Page ", page.Text);

        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好, 第页", page.Text);

        input.Text = "7";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好, 第7页", page.Text);

        window.Close();
    }

    [Fact]
    public void LocalizeFormat_ProvideValue_WhenFormatKeyIsNull_Returns_Binding()
    {
        var extension = new FormatTranslateExtension();
        Assert.IsType<MultiBinding>(extension.ProvideValue(null!));
    }

    [Fact]
    public void LocalizeFormat_ProvideValue_WhenFormatKeyDoesNotExist_Returns_EmptyBinding()
    {
        var extension = new FormatTranslateExtension
        {
            FormatKey = new LinguaKey("NoSuchFormatKey", TestLanguageManager.Instance)
        };

        Assert.IsType<MultiBinding>(extension.ProvideValue(null!));
    }

    [AvaloniaFact]
    public void LocalizeFormat_WhenItemHasNullKeyAndNullBinding_UsesEmptyPlaceholderAndStillUpdatesOnCultureChange()
    {
        TestLanguageManager.Instance.Reset();

        var extension = new FormatTranslateExtension
        {
            FormatKey = TestLanguageManager.Keys.Format_Template,
            Items =
            [
                new TranslateEntry { Key = TestLanguageManager.Keys.Greeting_Message },
                new TranslateEntry()
            ]
        };

        var binding = Assert.IsType<MultiBinding>(extension.ProvideValue(null!));
        var textBlock = new TextBlock();
        textBlock.Bind(TextBlock.TextProperty, binding);

        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello, Page (unset)", textBlock.Text);

        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好, 第(unset)页", textBlock.Text);
    }

    [Fact]
    public void LocalizeFormat_ProvideValue_AppendsManagerCultureBinding()
    {
        var extension = new FormatTranslateExtension
        {
            FormatKey = TestLanguageManager.Keys.Format_Template,
            Items =
            [
                new TranslateEntry { Key = TestLanguageManager.Keys.Greeting_Message },
                new TranslateEntry()
            ]
        };

        var binding = Assert.IsType<MultiBinding>(extension.ProvideValue(null!));

        // format template + 2 entries + trailing manager culture = 4
        Assert.Equal(4, binding.Bindings.Count);
        // the trailing culture binding is an observable binding just like the
        // leading format-template binding
        Assert.Equal(binding.Bindings[0].GetType(), binding.Bindings[^1].GetType());
    }

    [AvaloniaFact]
    public void LocalizeFormat_NumberFormatting_FollowsManagerCultureInsteadOfThreadCulture()
    {
        // An isolated manager keeps this test free of shared-singleion state
        // so it can safely run in parallel with the other test classes.
        var manager = new MiniManager()
            .Add(CultureInfo.InvariantCulture, ("NumberTemplate", "Value: {0:F1}"))
            .Add(new CultureInfo("fr-FR"), ("NumberTemplate", "Valeur : {0:F1}"));

        var vm = new NumberViewModel();
        var extension = new FormatTranslateExtension
        {
            FormatKey = new LinguaKey("NumberTemplate", manager),
            Items = [new TranslateEntry { Binding = vm.ValueObservable.ToBinding() }]
        };

        var textBlock = new TextBlock();
        textBlock.Bind(TextBlock.TextProperty, Assert.IsType<MultiBinding>(extension.ProvideValue(null!)));
        var window = new Window { Content = textBlock, Width = 200, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Value: 1.5", textBlock.Text);

        manager.UpdateCulture(new CultureInfo("fr-FR"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Valeur : 1,5", textBlock.Text);

        // dynamic argument updates keep using the manager culture
        vm.Value = 2.5m;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Valeur : 2,5", textBlock.Text);

        window.Close();
    }

    private sealed class NumberViewModel : INotifyPropertyChanged
    {
        private decimal _value = 1.5m;

        public event PropertyChangedEventHandler? PropertyChanged;

        public decimal Value
        {
            get => _value;
            set
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public IObservable<object?> ValueObservable =>
            this.ObserveProperty(nameof(Value), () => Value).Box();
    }

    /// <summary>
    /// A minimal isolated <see cref="ILinguaManager"/> for tests that need to
    /// mutate manager state without touching the shared
    /// <see cref="TestLanguageManager"/> singleton while tests run in parallel.
    /// </summary>
    private sealed class MiniManager : ILinguaManager
    {
        private readonly Dictionary<string, LinguaObservableString> _observables = new(StringComparer.Ordinal);
        private readonly LinguaRuntimeResources _resources = new();
        private readonly LinguaObservable<CultureInfo> _cultureChanges = new(string.Empty, CultureInfo.InvariantCulture);
        private CultureInfo _currentCulture = CultureInfo.InvariantCulture;

        public CultureInfo CurrentCulture => _currentCulture;

        public IObservable<CultureInfo> CultureChanges => _cultureChanges;

        public MiniManager Add(CultureInfo culture, params (string Key, string Value)[] entries)
        {
            _resources.Add(culture, entries.ToDictionary(e => e.Key, e => e.Value));

            foreach (var (key, value) in entries)
            {
                if (_observables.ContainsKey(key)) continue;
                var initial = culture.Equals(CultureInfo.InvariantCulture) ? value : string.Empty;
                _observables[key] = new LinguaObservableString(key, initial);
            }

            return this;
        }

        public void UpdateCulture(CultureInfo culture)
        {
            if (culture is null) culture = CultureInfo.InvariantCulture;

            _currentCulture = culture;
            _cultureChanges.OnNext(culture);

            var dict = _resources.Resolve(culture);
            if (dict is null) return;

            foreach (var observable in _observables.Values)
            {
                if (dict.TryGetValue(observable.Key, out var value))
                    observable.OnNext(value);
            }
        }

        public IObservable<string?>? GetObservable(string key) =>
            _observables.TryGetValue(key, out var observable) ? observable : null;

        public void AddResources(CultureInfo culture, IReadOnlyDictionary<string, string> resources) =>
            _resources.Add(culture, resources);

        public IReadOnlyDictionary<CultureInfo, string> GetTranslations(LinguaObservableString observable)
        {
            ArgumentNullException.ThrowIfNull(observable);

            var result = new Dictionary<CultureInfo, string>();
            foreach (var pair in _resources.GetAllValuesForKey(observable.Key))
                result[pair.Key] = pair.Value;
            return result;
        }

        public void ClearRuntimeResources() => _resources.Clear();
    }
}
