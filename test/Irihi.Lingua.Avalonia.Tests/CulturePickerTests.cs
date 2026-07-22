using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Headless.XUnit;
using Irihi.Lingua;
using Xunit;

namespace Irihi.Lingua.Avalonia.Tests;

public class CulturePickerTests
{
    [AvaloniaFact]
    public void SelectedItem_DefaultsToNull()
    {
        var picker = new CulturePicker();
        Assert.Null(picker.SelectedItem);
    }

    [AvaloniaFact]
    public void Managers_DefaultsToEmpty()
    {
        var picker = new CulturePicker();
        Assert.NotNull(picker.Managers);
        Assert.Empty(picker.Managers!);
    }

    [AvaloniaFact]
    public void Cultures_DefaultsToEmpty()
    {
        var picker = new CulturePicker();
        Assert.NotNull(picker.Cultures);
        Assert.Empty(picker.Cultures!);
    }

    [AvaloniaFact]
    public void SettingCultures_UpdatesCollection()
    {
        var picker = new CulturePicker();
        var cultures = new List<LinguaCulture>
        {
            new() { Culture = new CultureInfo("en") },
            new() { Culture = new CultureInfo("zh-Hans") }
        };

        picker.Cultures = cultures;

        Assert.Equal(2, picker.Cultures!.Count);
    }

    [AvaloniaFact]
    public void SettingSelectedItem_FiresCultureChanged()
    {
        var picker = new CulturePicker();
        var zhHans = new CultureInfo("zh-Hans");
        var lc = new LinguaCulture { Culture = zhHans };

        picker.Cultures = new List<LinguaCulture> { lc };

        LinguaCulture? received = null;
        picker.CultureChanged += (_, e) => received = e.Culture;

        picker.SelectedItem = lc;

        Assert.Same(lc, received);
    }

    [AvaloniaFact]
    public void SelectedItem_DrivesManagerUpdateCulture()
    {
        TestLanguageManager.Instance.Reset();
        var picker = new CulturePicker();
        var zhHans = new CultureInfo("zh-Hans");
        var lc = new LinguaCulture { Culture = zhHans };

        picker.Managers = new List<ILinguaManager> { TestLanguageManager.Instance };
        picker.Cultures = new List<LinguaCulture> { lc };

        picker.SelectedItem = lc;

        Assert.Equal(zhHans.Name, TestLanguageManager.Instance.CurrentCulture.Name);
    }

    [AvaloniaFact]
    public void SelectedItem_UpdatesMultipleManagers()
    {
        TestLanguageManager.Instance.Reset();
        var picker = new CulturePicker();
        var zhHans = new CultureInfo("zh-Hans");
        var lc = new LinguaCulture { Culture = zhHans };

        picker.Managers = new List<ILinguaManager>
        {
            TestLanguageManager.Instance,
            TestLanguageManager.Instance
        };
        picker.Cultures = new List<LinguaCulture> { lc };

        picker.SelectedItem = lc;

        Assert.Equal(zhHans.Name, TestLanguageManager.Instance.CurrentCulture.Name);
    }

    [AvaloniaFact]
    public void SettingSelectedItem_Null_DoesNotCrash()
    {
        var picker = new CulturePicker();
        var ex = Record.Exception(() => picker.SelectedItem = null);
        Assert.Null(ex);
    }

    [AvaloniaFact]
    public void ObservableCollection_Add_CulturesReflected()
    {
        var picker = new CulturePicker();
        var cultures = new ObservableCollection<LinguaCulture>();
        picker.Cultures = cultures;

        cultures.Add(new LinguaCulture { Culture = new CultureInfo("en") });
        cultures.Add(new LinguaCulture { Culture = new CultureInfo("fr") });

        Assert.Equal(2, picker.Cultures!.Count);
    }
}
