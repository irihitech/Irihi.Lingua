using Avalonia;
using Avalonia.Headless;
using Irihi.Lingua.Avalonia.Tests;

// Required by Avalonia.Headless.XUnit — points to the AppBuilder factory.
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Irihi.Lingua.Avalonia.Tests;

/// <summary>
/// Minimal Avalonia application used exclusively by the headless test runner.
/// No themes, no resources — just enough to satisfy AppBuilder.Configure.
/// </summary>
internal sealed class TestApp : Application { }

/// <summary>
/// Factory that <see cref="AvaloniaTestApplicationAttribute"/> uses to build
/// the headless application for each test run.
/// </summary>
public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
