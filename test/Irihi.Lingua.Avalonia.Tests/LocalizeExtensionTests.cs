using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Irihi.Lingua.Avalonia.Tests.ViewModels;
using Irihi.Lingua.Avalonia.Tests.Views;
using Irihi.Lingua.Extensions;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

/// <summary>
/// Headless Avalonia tests for <see cref="LocalizeExtension"/>.
///
/// Each test follows the same MVVM pattern as the demo project:
/// <list type="number">
///   <item>A <see cref="LocalizeView"/> UserControl (AXAML with two TextBlocks)</item>
///   <item>A <see cref="LocalizeViewModel"/> ViewModel (exposes <see cref="IObservable{T}"/> properties)</item>
///   <item>A headless <see cref="Window"/> hosting the view</item>
/// </list>
///
/// One TextBlock uses <c>{Binding GreetingMessage^}</c> (ViewModel stream binding),
/// the other uses <c>{Localize {x:Static …}}</c> (LocalizeExtension directly).
/// Both must reflect the active culture when <see cref="TestLanguageManager.UpdateCulture"/> is called.
/// </summary>
public class LocalizeExtensionTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates and shows a headless window hosting a <see cref="LocalizeView"/>
    /// with a fresh <see cref="LocalizeViewModel"/> as the DataContext.
    /// Returns the window and the two named TextBlocks for assertion.
    /// </summary>
    private static (Window Window, TextBlock AppTitleBound, TextBlock GreetingMessageBound, TextBlock LocalizeBound)
        CreateAndShowWindow()
    {
        var vm   = new LocalizeViewModel();
        var view = new LocalizeView { DataContext = vm };
        var window = new Window
        {
            Content = view,
            Width   = 400,
            Height  = 200,
            SizeToContent = SizeToContent.Manual
        };
        window.Show();

        // Flush any binding initialisation dispatched to the UI queue.
        Dispatcher.UIThread.RunJobs();

        var appTitle    = view.Find<TextBlock>("AppTitleBound")!;
        var greeting    = view.Find<TextBlock>("GreetingMessageBound")!;
        var localizeExt = view.Find<TextBlock>("LocalizeBound")!;

        return (window, appTitle, greeting, localizeExt);
    }

    // ── initial values ────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void LocalizeView_OnLoad_ShowsDefaultCultureValues()
    {
        TestLanguageManager.Instance.Reset();
        var (window, appTitle, greeting, localizeExt) = CreateAndShowWindow();

        Assert.Equal("Lingua Headless Test", appTitle.Text);
        Assert.Equal("Hello",               greeting.Text);
        Assert.Equal("Hello",               localizeExt.Text);

        window.Close();
    }

    // ── culture switch: en → zh ───────────────────────────────────────────────

    [AvaloniaFact]
    public void LocalizeView_AfterSwitchToChinese_BothTextBlocksUpdateToChineseValues()
    {
        TestLanguageManager.Instance.Reset();
        var (window, appTitle, greeting, localizeExt) = CreateAndShowWindow();

        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Dispatcher.UIThread.RunJobs();

        // ViewModel stream binding
        Assert.Equal("Lingua 无头测试", appTitle.Text);
        Assert.Equal("你好",            greeting.Text);

        // LocalizeExtension binding
        Assert.Equal("你好", localizeExt.Text);

        window.Close();
    }

    // ── culture switch: zh → en ───────────────────────────────────────────────

    [AvaloniaFact]
    public void LocalizeView_AfterSwitchBackToDefault_TextBlocksRevertToEnglishValues()
    {
        TestLanguageManager.Instance.Reset();
        var (window, _, greeting, localizeExt) = CreateAndShowWindow();

        // To Chinese
        TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("你好", greeting.Text);
        Assert.Equal("你好", localizeExt.Text);

        // Back to default
        TestLanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello", greeting.Text);
        Assert.Equal("Hello", localizeExt.Text);

        window.Close();
    }

    // ── multiple culture switches ─────────────────────────────────────────────

    [AvaloniaFact]
    public void LocalizeView_MultipleCultureTogglesCyclesCorrectly()
    {
        TestLanguageManager.Instance.Reset();
        var (window, _, greeting, localizeExt) = CreateAndShowWindow();

        for (int i = 0; i < 3; i++)
        {
            TestLanguageManager.Instance.UpdateCulture(new CultureInfo("zh-Hans"));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("你好", greeting.Text);
            Assert.Equal("你好", localizeExt.Text);

            TestLanguageManager.Instance.UpdateCulture(CultureInfo.InvariantCulture);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Hello", greeting.Text);
            Assert.Equal("Hello", localizeExt.Text);
        }

        window.Close();
    }

    // ── LocalizeExtension null-key guard ──────────────────────────────────────

    [AvaloniaFact]
    public void LocalizeExtension_ProvideValue_WhenKeyIsNull_ReturnsUnsetValue()
    {
        var ext = new LocalizeExtension();   // Key intentionally left null
        Assert.Equal(AvaloniaProperty.UnsetValue, ext.ProvideValue(null!));
    }

    [AvaloniaFact]
    public void LocalizeExtension_ProvideValue_WhenKeyNotFoundInManager_ReturnsUnsetValue()
    {
        var key = new LinguaKey("NoSuchKey", TestLanguageManager.Instance);
        var ext = new LocalizeExtension(key);
        Assert.Equal(AvaloniaProperty.UnsetValue, ext.ProvideValue(null!));
    }
}
