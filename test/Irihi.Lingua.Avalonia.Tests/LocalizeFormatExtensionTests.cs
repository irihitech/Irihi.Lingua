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

public class LocalizeFormatExtensionTests
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
        var extension = new LocalizeFormatExtension();
        Assert.IsType<MultiBinding>(extension.ProvideValue(null!));
    }

    [Fact]
    public void LocalizeFormat_ProvideValue_WhenFormatKeyDoesNotExist_Returns_EmptyBinding()
    {
        var extension = new LocalizeFormatExtension
        {
            FormatKey = new LinguaKey("NoSuchFormatKey", TestLanguageManager.Instance)
        };

        Assert.IsType<MultiBinding>(extension.ProvideValue(null!));
    }

    [AvaloniaFact]
    public void LocalizeFormat_WhenItemHasNullKeyAndNullBinding_UsesEmptyPlaceholderAndStillUpdatesOnCultureChange()
    {
        TestLanguageManager.Instance.Reset();

        var extension = new LocalizeFormatExtension
        {
            FormatKey = TestLanguageManager.Keys.Format_Template,
            Items =
            [
                new LocalizeItem { Key = TestLanguageManager.Keys.Greeting_Message },
                new LocalizeItem()
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
}
